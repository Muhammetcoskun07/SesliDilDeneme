using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Data.Context;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class MessageService : Service<Message>, IService<Message>
    {
        private readonly IRepository<Message> _messageRepository;
        private readonly IMapper _mapper;
        private readonly SesliDilDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IRepository<AIAgent> _agentRepository;
        private readonly IRepository<User> _userRepository;
        private readonly TtsService _ttsService;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            TtsService ttsService,
            SesliDilDbContext context,
            IRepository<Message> messageRepository,
            IMapper mapper,
            HttpClient httpClient,
            IConfiguration configuration,
            IRepository<User> userRepository,
            ILogger<MessageService> logger,
            IRepository<AIAgent> agentRepository)
            : base(messageRepository, mapper)
        {
            _userRepository = userRepository;
            _messageRepository = messageRepository;
            _mapper = mapper;
            _httpClient = httpClient;
            _configuration = configuration;
            _ttsService = ttsService;
            _logger = logger;
            _agentRepository = agentRepository;
            _context = context;
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _configuration["OpenAI:ApiKey"]);


        }

        public async Task<MessageDto> SendMessageAsync(SendMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.AgentId))
                throw new ArgumentException("Invalid input");

            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                throw new ArgumentException("User not found");

            if (string.IsNullOrWhiteSpace(user.TargetLanguage) || string.IsNullOrWhiteSpace(user.NativeLanguage))
                throw new ArgumentException("User's languages are not specified");

            string learningGoals = user.LearningGoals != null ? JsonSerializer.Serialize(user.LearningGoals) : "";
            string improvementGoals = user.ImprovementGoals != null ? JsonSerializer.Serialize(user.ImprovementGoals) : "";
            string topicInterests = user.TopicInterests != null ? JsonSerializer.Serialize(user.TopicInterests) : "";

            var agent = await _agentRepository.GetByIdAsync(request.AgentId);
            if (agent == null || !agent.IsActive)
                throw new ArgumentException("Invalid or inactive agent");

            // Prompt
            var prompt = $@"
You are a helpful language tutor.

The learner's message: ""{request.Content}""

Tasks:
1. Identify **only real grammar mistakes** in the learner's original message in {user.TargetLanguage}. 
   -Focus ONLY on grammar mistakes. Do NOT consider missing or extra punctuation marks (periods, commas, question marks) as errors. Ignore all punctuation issues.
   - Focus only on grammar issues (verb conjugation, word order, articles, plural/singular, agreement, etc.).
   - Ignore all punctuation issues completely. Do NOT mark missing or extra punctuation as grammar errors.
2. Provide the corrected version of the learner's message in {user.TargetLanguage} without changing its meaning.
3. Respond to the corrected message in {user.TargetLanguage}.
4. Translate your response into the learner's native language ({user.NativeLanguage}).

Return strictly JSON in this exact format:
{{
  ""correctedText"": ""..."",
  ""aiText"": ""..."",
  ""translatedContent"": ""..."",
  ""grammarErrors"": [""..."", ""...""]
}}
Always fill 'grammarErrors' with every issue found. If there are no mistakes, return an empty array.Ignore all punctuation issues completely.
";

            // system prompt
            var messages = new List<object>
    {
        new
        {
            role = "system",
            content = $@"{agent.AgentPrompt ?? ""}

Here is the learner's profile:
- Native language: {user.NativeLanguage}
- Target language: {user.TargetLanguage}
- Proficiency level: {user.ProficiencyLevel}
- Age range: {user.AgeRange}
- Weekly speaking goal: {user.WeeklySpeakingGoal}
- Learning goals: {learningGoals}
- Improvement goals: {improvementGoals}
- Topic interests: {topicInterests}

Tailor your answers to their goals and preferences."
        }
    };

            // 🔹 Son 5 mesajı (user + assistant) al ve ekle
            var lastMessages = await GetLastMessagesAsync(request.ConversationId, 5);
            foreach (var msg in lastMessages)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            // Kullanıcının yeni mesajını ekle
            messages.Add(new { role = "user", content = prompt });

            var requestBody = new
            {
                model = "gpt-4o",
                messages = messages,
                temperature = 0.7
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            var jsonText = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "{}";

            jsonText = jsonText.Trim();
            if (jsonText.StartsWith("```"))
            {
                int firstBrace = jsonText.IndexOf('{');
                int lastBrace = jsonText.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                    jsonText = jsonText.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonText);

            var correctedText = parsed["correctedText"].GetString();
            var aiText = parsed["aiText"].GetString();
            var translatedContent = parsed["translatedContent"].GetString();
            var grammarErrors = parsed["grammarErrors"].EnumerateArray().Select(e => e.GetString()).ToList();

            // Kullanıcı mesajını kaydet
            var userMessage = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = request.ConversationId,
                Role = "user",
                Content = request.Content,
                CorrectedText = correctedText,
                GrammarErrors = grammarErrors,
                CreatedAt = DateTime.UtcNow,
                SpeakerType = "user"
            };
            await CreateAsync(userMessage);

            // AI mesajı -> TTS
            var voice = GetVoiceByLanguage(user.TargetLanguage);
            var audioBytes = await _ttsService.ConvertTextToSpeechAsync(aiText, voice);
            var audioUrl = await _ttsService.SaveAudioToFileAsync(audioBytes);

            // AI mesajını kaydet
            var aiMessage = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = userMessage.ConversationId,
                Role = "assistant",
                Content = aiText,
                TranslatedContent = translatedContent,
                AudioUrl = audioUrl,
                CreatedAt = DateTime.UtcNow,
                SpeakerType = "assistant",
            };
            await CreateAsync(aiMessage);

            var responseDto = _mapper.Map<MessageDto>(aiMessage);
            responseDto.CorrectedText = correctedText;
            responseDto.GrammarErrors = grammarErrors;
            return responseDto;
        }


        private string GetVoiceByLanguage(string language) => "alloy";
        public async Task<IEnumerable<MessageDto>> GetMessagesByConversationIdAsync(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentNullException(nameof(conversationId), "Invalid conversationId");

            var messages = await _messageRepository.GetAllAsync();
            var filtered = messages.Where(m => m.ConversationId == conversationId);
            return _mapper.Map<IEnumerable<MessageDto>>(filtered);
        }
        public async Task<List<Message>> GetLastMessagesAsync(string conversationId, int count = 5)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(count)
                .OrderBy(m => m.CreatedAt) 
                .ToListAsync();
        }
        public async Task<List<Message>> GetAllMessagesAsync(string conversationId)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt) // Eski -> yeni
                .ToListAsync();
        }
    }



}