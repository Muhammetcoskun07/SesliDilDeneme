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
        private readonly IRepository<Conversation> _conversationRepository;
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
            IRepository<Conversation> conversationRepository,
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
            _conversationRepository = conversationRepository;
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

            // 🔹 Sistem prompt: kullanıcı profili ve genel talimat
            var systemPrompt = $@" 
{agent.AgentPrompt ?? ""}
You are a helpful language tutor. And your name is {agent.AgentName ?? "Language Tutor"}.

Learner profile:
- Native language: {user.NativeLanguage}
- Target language: {user.TargetLanguage}
- Proficiency level: {user.ProficiencyLevel}
- Age range: {user.AgeRange}
- Weekly speaking goal: {user.WeeklySpeakingGoal}
- Learning goals: {learningGoals}
- Improvement goals: {improvementGoals}
- Topic interests: {topicInterests}

Instructions:
- Only check grammar errors in the NEW user message.Ignore capitalization and punctuation completely.
- Provide the corrected version if there are grammar errors.
- Respond naturally in the target language.
- Translate your response to the learner's native language.
- Ignore punctuation errors.
- Return strictly JSON:
{{
  ""correctedText"": ""..."",
  ""aiText"": ""..."",
  ""translatedContent"": ""..."",
  ""grammarErrors"": [""..."", ""...""]
}}
Always return 'correctedText' as empty string "" if there are no errors.
";

            // 🔹 Mesaj listesi
            var messages = new List<object>
    {
        new { role = "system", content = systemPrompt }
    };

            // 🔹 Son 5 mesajı al ve ekle
            var lastMessages = await GetLastMessagesAsync(request.ConversationId, 5);
            foreach (var msg in lastMessages)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            // 🔹 Kullanıcının yeni mesajını doğrudan ekle
            messages.Add(new { role = "user", content = request.Content });

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

            // 🔹 Kod blokları varsa JSON'u çıkart
            jsonText = jsonText.Trim();
            if (jsonText.StartsWith("```"))
            {
                int firstBrace = jsonText.IndexOf('{');
                int lastBrace = jsonText.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                    jsonText = jsonText.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            Dictionary<string, JsonElement>? parsed = null;

            try
            {
                parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonText);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parse hatası: {ex.Message}");
                Console.WriteLine($"Gelen içerik: {jsonText}");
            }

            var correctedText = parsed.ContainsKey("correctedText") ? parsed["correctedText"].GetString() ?? "" : "";
            var aiText = parsed.ContainsKey("aiText") ? parsed["aiText"].GetString() ?? "" : "";
            var translatedContent = parsed.ContainsKey("translatedContent") ? parsed["translatedContent"].GetString() ?? "" : "";
            var grammarErrors = parsed.ContainsKey("grammarErrors")
                ? parsed["grammarErrors"].EnumerateArray().Select(e => e.GetString()).Where(s => s != null).ToList()
                : new List<string>();

            // 🔹 Kullanıcı mesajını kaydet
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

            // 🔹 AI mesajını TTS ile oluştur
            var voice = GetVoiceByLanguage(user.TargetLanguage);
            var audioBytes = await _ttsService.ConvertTextToSpeechAsync(aiText, voice);
            var audioUrl = await _ttsService.SaveAudioToFileAsync(audioBytes);

            var aiMessage = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = request.ConversationId,
                Role = "assistant",
                Content = aiText,
                TranslatedContent = translatedContent,
                AudioUrl = audioUrl,
                CreatedAt = DateTime.UtcNow,
                SpeakerType = "assistant",
                CorrectedText = correctedText,
                GrammarErrors = grammarErrors
            };
            await CreateAsync(aiMessage);

            // 🔹 DTO oluştur ve dön
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
        public async Task<string> SpeakMessageAsync(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                throw new ArgumentException("MessageId zorunludur.");

            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message == null)
                throw new KeyNotFoundException("Mesaj bulunamadı.");

            var conversation = await _conversationRepository.GetByIdAsync(message.ConversationId);
            if (conversation == null)
                throw new KeyNotFoundException("Conversation bulunamadı.");

            var user = await _userRepository.GetByIdAsync(conversation.UserId);
            if (user == null)
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            if (string.IsNullOrWhiteSpace(user.TargetLanguage))
                throw new ArgumentException("Kullanıcının hedef dili ayarlı değil.");

            var audioBytes = await _ttsService.ConvertTextToSpeechAsync(message.Content, "alloy");
            var relativeUrl = await _ttsService.SaveAudioToFileAsync(audioBytes);
            var baseUrl = _configuration["AppUrl"]?.TrimEnd('/') ?? "";

            return $"{baseUrl}{relativeUrl}";
        }
        public async Task<MessageDto> CreateMessageAsync(MessageDto messageDto)
        {
            if (messageDto == null)
                throw new ArgumentException("Message verisi zorunludur.");

            var message = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = messageDto.ConversationId,
                Role = messageDto.Role,
                Content = messageDto.Content,
                AudioUrl = messageDto.AudioUrl ?? "",
                SpeakerType = messageDto.SpeakerType,
                CreatedAt = DateTime.UtcNow,
                TranslatedContent = messageDto.TranslatedContent ?? "",
                GrammarErrors = messageDto.GrammarErrors ?? new List<string>()
            };

            await CreateAsync(message);
            return _mapper.Map<MessageDto>(message);
        }
        public async Task<TranslatedContentResponse> GetTranslatedMessageAsync(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                throw new ArgumentException("MessageId zorunludur.");

            var message = await GetByIdAsync<string>(messageId);
            if (message == null)
                throw new KeyNotFoundException("Mesaj bulunamadı.");

            return new TranslatedContentResponse
            {
                TranslatedContent = message.TranslatedContent
            };
        }
        public async Task DeleteMessageAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz mesaj kimliği.");

            var message = await GetByIdAsync<string>(id);
            if (message == null)
                throw new KeyNotFoundException("Mesaj bulunamadı.");

            await DeleteAsync(message);
        }

    }



}