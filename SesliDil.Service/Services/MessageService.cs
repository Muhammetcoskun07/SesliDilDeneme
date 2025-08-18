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
   - Ignore missing punctuation (periods, commas, question marks).
   - Focus only on grammar issues (verb conjugation, word order, articles, plural/singular, agreement, etc.).
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
Always fill 'grammarErrors' with every issue found. If there are no mistakes, return an empty array.
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


        private string GetVoiceByLanguage(string language)
        {
            return language.ToLower() switch
            {
                "turkish" => "alloy",
                "english" => "nova",
                "spanish" => "shimmer",
                // Diğer diller için eklemeler yapabilirsin
                _ => "alloy" // Varsayılan ses
            };
        }
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
                .OrderBy(m => m.CreatedAt) // sırayı koru (eski -> yeni)
                .ToListAsync();
        }
        public async Task<MessageDto> CreateMessageAsync(string conversationId, string role, string content, string audioUrl)
        {
            if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(role) || string.IsNullOrEmpty(content))
                throw new ArgumentNullException("Invalid input");

            var message = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Role = role,
                Content = content,
                AudioUrl = audioUrl,
                SpeakerType = role,
                CreatedAt = DateTime.UtcNow
            };

            await CreateAsync(message);
            return _mapper.Map<MessageDto>(message);
        }




        public async Task<string> GenerateSpeechAsync(string text)
        {
            var requestBody = new
            {
                model = "tts-1",
                input = text,
                voice = "alloy"
            };

            var response = await _httpClient.PostAsJsonAsync("audio/speech", requestBody);
            response.EnsureSuccessStatusCode();

            var audioStream = await response.Content.ReadAsStreamAsync();
            return "https://your-storage-bucket/speech.mp3";
        }

        public async Task<string> TranslateAsync(string text, string nativeLanguage, string agentId)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent == null || !agent.IsActive)
                throw new ArgumentException("Invalid or inactive agent", nameof(agentId));

            var prompt = $"{agent.AgentPrompt}\nTranslate the following into {nativeLanguage}:\n{text}";

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
            new { role = "system", content = "You are a helpful translator." },
            new { role = "user", content = prompt }
        },
                temperature = 0.5
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }

        public async Task<List<string>> CheckGrammarAsync(string text, string agentId)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();

            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent == null || !agent.IsActive)
                throw new ArgumentException("Invalid or inactive agent", nameof(agentId));

            var prompt = $"{agent.AgentPrompt}\nIdentify and list all grammar mistakes in the following text. Return only the errors:\n{text}";

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = "You are a grammar checking assistant." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            var errorsText = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";

            var errors = errorsText
                .Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .ToList();

            return errors;
        }

        private async Task<byte[]> DownloadAudioAsync(string audioUrl)
        {
            using var client = new HttpClient();
            return await client.GetByteArrayAsync(audioUrl);
        }
        public async Task<string> GetAIResponseAsync(string userInput, string targetLanguage, string agentId, string conversationId, string userId)
        {
            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent == null || !agent.IsActive)
                throw new ArgumentException("Invalid or inactive agent", nameof(agentId));
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found", nameof(userId));

            // JSON alanlarını string'e çevir
            string learningGoals = user.LearningGoals != null ? JsonSerializer.Serialize(user.LearningGoals) : "";
            string improvementGoals = user.ImprovementGoals != null ? JsonSerializer.Serialize(user.ImprovementGoals) : "";
            string topicInterests = user.TopicInterests != null ? JsonSerializer.Serialize(user.TopicInterests) : "";

            var promptMessages = new List<object>
{
    new
    {
        role = "system",
        content = $@"{agent.AgentPrompt}
You are a helpful language tutor responding in {targetLanguage}.
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

            promptMessages.Add(new { role = "user", content = userInput });

            var requestBody = new
            {
                model = "gpt-4o",
                messages = promptMessages,
                temperature = 0.7
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
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