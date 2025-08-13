using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class MessageService : Service<Message>, IService<Message>
    {
        private readonly IRepository<Message> _messageRepository;
        private readonly IMapper _mapper;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IRepository<AIAgent> _agentRepository;
        private readonly IRepository<User> _userRepository;
        private readonly TtsService _ttsService;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            TtsService ttsService,
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

            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _configuration["OpenAI:ApiKey"]);
        }

        public async Task<MessageDto> SendMessageAsync(SendMessageRequest request)
        {
            _logger.LogInformation($"SendMessageAsync called with ConversationId={request.ConversationId}, UserId={request.UserId}, AgentId={request.AgentId}, Content={request.Content}");

            if (request == null || string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.AgentId))
            {
                _logger.LogWarning("Invalid input in SendMessageAsync");
                throw new ArgumentException("Invalid input");
            }
            if (string.IsNullOrWhiteSpace(request.ConversationId))
            {
                _logger.LogError("ConversationId is missing!");
            }
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                throw new ArgumentException("User not found");

            if (string.IsNullOrWhiteSpace(user.TargetLanguage))
                throw new ArgumentException("User's target language is not specified");

            if (string.IsNullOrWhiteSpace(user.NativeLanguage))
                throw new ArgumentException("User's native language is not specified");

            // Kullanıcının gönderdiği mesajı, native diline çeviriyoruz (örneğin: kullanıcı Türkçe yazıyorsa, nativeLanguage = "tr")
            var userMessage = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = request.ConversationId,
                Role = "user",
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                SpeakerType = "user",
                GrammarErrors = new List<string>(),
                TranslatedContent = await TranslateAsync(request.Content, user.NativeLanguage, request.AgentId)
            };

            userMessage.GrammarErrors = await CheckGrammarAsync(request.Content, request.AgentId);
            await CreateAsync(userMessage);

            // AI cevabını kullanıcı hedef dilinde alıyoruz (örneğin "es" İspanyolca)
            var aiResponseText = await GetAIResponseAsync(request.Content, user.TargetLanguage, request.AgentId, request.ConversationId);

            // AI cevabını kullanıcının native diline çeviriyoruz (örneğin İngilizce)
            var translatedContent = await TranslateAsync(aiResponseText, user.NativeLanguage, request.AgentId);

            // TTS → Byte[] ses dosyası al, burada hedef dili kullanabiliriz
            var voice = GetVoiceByLanguage(user.TargetLanguage); // Mesela "spanish" için "shimmer"
            var audioBytes = await _ttsService.ConvertTextToSpeechAsync(aiResponseText, voice);

            // Byte[] → mp3 olarak kaydet ve URL al
            var audioUrl = await _ttsService.SaveAudioToFileAsync(audioBytes);

            var aiMessage = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = userMessage.ConversationId,
                Role = "assistant",
                Content = aiResponseText,              // AI cevabı hedef dilde
                CreatedAt = DateTime.UtcNow,
                SpeakerType = "assistant",
                GrammarErrors = new List<string>(),
                TranslatedContent = translatedContent, // AI cevabının native dil çevirisi
                AudioUrl = audioUrl
            };

            await CreateAsync(aiMessage);

            return _mapper.Map<MessageDto>(aiMessage);
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

        public async Task<string> ConvertSpeechToTextAsync(string audioUrl)
        {
            var audioContent = await DownloadAudioAsync(audioUrl);

            using var content = new MultipartFormDataContent
    {
        { new StreamContent(new MemoryStream(audioContent)), "file", "audio.mp3" },
        { new StringContent("whisper-1"), "model" }
    };

            var response = await _httpClient.PostAsync("audio/transcriptions", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TranscriptionResponse>();
            return result.Text;
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
        public async Task<string> GetAIResponseAsync(string userInput, string targetLanguage, string agentId, string conversationId)
        {
            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent == null || !agent.IsActive)
                throw new ArgumentException("Invalid or inactive agent", nameof(agentId));

            // Konuşma geçmişini al
            var messages = await _messageRepository.GetAllAsync();
           

            // Sistem prompt'u ve konuşma geçmişini birleştir
            var promptMessages = new List<object>
            {
                new { role = "system", content = $"{agent.AgentPrompt}\nYou are a helpful assistant responding in {targetLanguage}." }
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

    }



}