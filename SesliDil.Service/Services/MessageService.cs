using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        public MessageService(
            IRepository<Message> messageRepository,
            IMapper mapper,
            HttpClient httpClient,
            IConfiguration configuration,
            IRepository<User> userRepository,
            IRepository<AIAgent> agentRepository)
            : base(messageRepository, mapper)
        {
            _userRepository = userRepository;
            _messageRepository = messageRepository;
            _mapper = mapper;
            _httpClient = httpClient;
            _configuration = configuration;
            _agentRepository = agentRepository;

            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _configuration["OpenAI:ApiKey"]);
        }

        public async Task<MessageDto> SendMessageAsync(SendMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AgentId))
                throw new ArgumentException("Invalid input");

            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                throw new ArgumentException("User not found");

            if (string.IsNullOrWhiteSpace(user.TargetLanguage))
                throw new ArgumentException("User's target language is not specified");

            string content = request.Content;

            // Ses varsa, sesi yazıya döndür
            if (string.IsNullOrWhiteSpace(content) && !string.IsNullOrWhiteSpace(request.AudioUrl))
            {
                content = await ConvertSpeechToTextAsync(request.AudioUrl);
                if (string.IsNullOrWhiteSpace(content))
                    throw new ArgumentException("Audio could not be converted to text");
            }

            var userMessage = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = request.ConversationId,
                Role = "user",
                Content = content,
                AudioUrl = request.AudioUrl,
                CreatedAt = DateTime.UtcNow,
                SpeakerType = "user",
                GrammarErrors = new List<string>(),
                TranslatedContent = await TranslateAsync(request.Content, user.NativeLanguage, request.AgentId) // Burada nativeLanguage kullanılıyor

            };

            userMessage.GrammarErrors = await CheckGrammarAsync(content, request.AgentId);
            await CreateAsync(userMessage);

            var aiResponseText = await GetAIResponseAsync(content, user.TargetLanguage, request.AgentId);

            var aiMessage = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = userMessage.ConversationId,
                Role = "assistant",
                Content = aiResponseText,
                CreatedAt = DateTime.UtcNow,
                SpeakerType = "assistant",
                GrammarErrors = new List<string>(),

                TranslatedContent = await TranslateAsync(aiResponseText, user.TargetLanguage, request.AgentId) // AI cevabı hedef dilde

            };

            await CreateAsync(aiMessage);

            return _mapper.Map<MessageDto>(aiMessage);
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

        public async Task<string> TranslateAsync(string text, string targetLanguage, string agentId)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent == null || !agent.IsActive)
                throw new ArgumentException("Invalid or inactive agent", nameof(agentId));

            var prompt = $"{agent.AgentPrompt}\nTranslate the following into {targetLanguage}:\n{text}";

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
        public async Task<string> GetAIResponseAsync(string userInput, string targetLanguage, string agentId)
        {
            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent == null || !agent.IsActive)
                throw new ArgumentException("Invalid or inactive agent", nameof(agentId));

            var prompt = $"{agent.AgentPrompt}\nUser: {userInput}\nPlease respond only in {targetLanguage}, with relevant and helpful information.";

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }
    }



}