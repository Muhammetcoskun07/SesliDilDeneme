using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
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

        public MessageService(IRepository<Message> messageRepository, IMapper mapper, HttpClient httpClient, IConfiguration configuration, IRepository<AIAgent> agentRepository)
            : base(messageRepository, mapper)
        {
            _messageRepository = messageRepository;
            _mapper = mapper;
            _httpClient = httpClient;
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _configuration["OpenAI:ApiKey"]);
            _agentRepository = agentRepository;
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesByConversationIdAsync(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentNullException("Invalid conversationId", nameof(conversationId));

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
                voice = "alloy" // OpenAI ses
            };
            var response = await _httpClient.PostAsJsonAsync("audio/speech", requestBody);
            response.EnsureSuccessStatusCode();
            var audioStream = await response.Content.ReadAsStreamAsync();
            // Gerçekte: Ses dosyasını S3’e yükleyip URL döndür
            return "https://your-storage-bucket/speech.mp3"; // Mock, S3 ile güncelle
        }

        public async Task<string> ConvertSpeechToTextAsync(string audioUrl)
        {
            var audioContent = await DownloadAudioAsync(audioUrl);
            using var content = new MultipartFormDataContent
            {
                { new StreamContent(new MemoryStream(audioContent)), "file", "audio.mp3" }
            };
            var response = await _httpClient.PostAsync("audio/transcriptions", content);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TranscriptionResponse>();
            return result.Text;
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage, string agentId)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // Agent’ın prompt’ını al
            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent == null || !agent.IsActive)
                throw new ArgumentException("Invalid or inactive agent", nameof(agentId));

            var prompt = agent.AgentPrompt ?? "Translate the following text accurately into the specified language: {0}";
            var fullPrompt = string.Format(prompt, text);
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new { role = "user", content = $"{fullPrompt} to {targetLanguage}" } },
                temperature = 0.7
            };
            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
            return result.Choices[0].Message.Content;
        }

        private async Task<byte[]> DownloadAudioAsync(string audioUrl)
        {
            using var client = new HttpClient();
            return await client.GetByteArrayAsync(audioUrl);
        }
        public async Task<List<string>> CheckGrammarAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();

            var prompt = "Identify and list all grammar errors in the following text, return only the errors as a list: {0}";
            var fullPrompt = string.Format(prompt, text);
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new { role = "user", content = fullPrompt } },
                temperature = 0.2 // 0.0-1.0 arasında 0a yaklaştıkç kesinlik artar
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
            var errorsText = result.Choices[0].Message.Content;

            // OpenAI'dan gelen metni listeye çevir
            var errors = errorsText.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim()).ToList();
            return errors.Any() ? errors : new List<string>();
        }
    }

    public class TranscriptionResponse
    {
        public string Text { get; set; }
    }

    public class TranslationResponse
    {
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        public Message Message { get; set; }
    }


}
