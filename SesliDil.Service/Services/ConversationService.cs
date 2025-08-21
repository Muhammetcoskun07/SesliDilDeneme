using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;   // <-- ToListAsync(), EF.Functions, ExecuteSqlRawAsync için
using Microsoft.Extensions.Configuration;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Data.Context;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class ConversationService : Service<Conversation>, IService<Conversation>
    {
        private readonly IRepository<Conversation> _conversationRepository;
        private readonly IMapper _mapper;
        private readonly SesliDilDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly MessageService _messageService;
        private readonly IConfiguration _configuration;
        private readonly IRepository<User> _userRepository;

        public ConversationService(IRepository<Conversation> conversationRepository, IMapper mapper,IConfiguration configuration, SesliDilDbContext dbContext,IRepository<User> userRepository, HttpClient httpClient,MessageService messageService)
            : base(conversationRepository, mapper)
        {
            _dbContext = dbContext;
            _conversationRepository = conversationRepository;
            _mapper = mapper;
            _configuration = configuration;
            _userRepository = userRepository;
            _httpClient = httpClient;
            _messageService = messageService;
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _configuration["OpenAI:ApiKey"]);
        }

        public async Task<IEnumerable<ConversationDto>> GetByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("Invalid User");

            var conversations = await _conversationRepository.GetAllAsync();
            var userConversations = conversations.Where(c => c.UserId == userId);

            return _mapper.Map<IEnumerable<ConversationDto>>(userConversations);
        }

        public async Task<ConversationSummaryDto> GetSummaryByConversationIdAsync(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
                throw new ArgumentException("Invalid Conversation Id");

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                throw new Exception("Conversation not found");

            return _mapper.Map<ConversationSummaryDto>(conversation);
        }

        public async Task<string> SaveAgentActivityAsync(
            string conversationId,
            string userId,
            string agentId,
            TimeSpan duration,
            int messageCount,
            int wordCount,
            double wordsPerMinute)
        {
            var activity = new ConversationAgentActivity
            {
                ActivityId = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                UserId = userId,
                AgentId = agentId,
                Duration = duration,
                MessageCount = messageCount,
                WordCount = wordCount,
                WordsPerMinute = wordsPerMinute,
            };

            await _dbContext.ConversationAgentActivities.AddAsync(activity);
            await _dbContext.SaveChangesAsync();
            return activity.ActivityId;
        }

        public async Task SaveSummaryAsync(string conversationId, string summary)
        {
            var conv = await _conversationRepository.GetByIdAsync(conversationId);
            if (conv == null) throw new Exception("Conversation bulunamadı");

            conv.Summary = summary;
            _conversationRepository.Update(conv);
            await _conversationRepository.SaveChangesAsync();
        }
       public async Task<int> DeleteEmptyConversationsAsync()
        {
           var deleted = await _dbContext.Database.ExecuteSqlRawAsync(@"
        DELETE FROM ""Conversation"" c
       WHERE NOT EXISTS (
    /       SELECT 1 FROM ""Message"" m WHERE m.""ConversationId"" = c.""ConversationId""
       )
    ");
           return deleted;
       }
        public async Task<ConversationSummaryResult> EndConversationAsync(string conversationId, bool forceEnd = true)
        {
            var conversation = await GetByIdAsync<string>(conversationId);
            if (conversation == null)
                throw new ArgumentException("Conversation not found");

            if (forceEnd)
            {
                var duration = DateTime.UtcNow - conversation.StartedAt;
                conversation.DurationMinutes = (int)duration.TotalMinutes;
            }

            await UpdateAsync(conversation);

            // Özet ve title alma
            var summaryResult = await GetConversationSummaryAsync(conversationId);

            return summaryResult;
        }

        public async Task<ConversationSummaryDto> BuildConversationSummaryComputedAsync(
          string conversationId, int sampleCount = 3, int highlightCount = 3)
        {
            if (string.IsNullOrEmpty(conversationId))
                throw new ArgumentException("Invalid Conversation Id");

            var conv = await _conversationRepository.GetByIdWithMessagesAsync(conversationId);
            if (conv == null)
                throw new Exception("Conversation not found");

            var messages = conv.Messages?.OrderBy(m => m.CreatedAt).ToList() ?? new List<Message>();

            // No messages
            if (messages.Count == 0)
            {
                return new ConversationSummaryDto
                {
                    ConversationId = conv.ConversationId,
                    Summary = conv.Summary ?? "",
                    DurationSeconds = 0,
                    MistakesCount = 0,
                    MistakeSamples = new List<MistakeSampleDto>(),
                    TotalWords = 0,
                    MessageCount = 0,
                    UserMessageCount = 0,
                    AgentMessageCount = 0,
                    StartedAtUtc = conv.StartedAt,
                    EndedAtUtc = conv.StartedAt,
                    Highlights = new List<string>()
                };
            }

            var first = messages.First().CreatedAt;
            var last = messages.Last().CreatedAt;
            var durationSec = Math.Max((int)(last - first).TotalSeconds, 1);

            // --- SUMMARY: konuşmadan otomatik üret ---
            string computedSummary = ComputeSummaryFromConversation(messages);
            if (string.IsNullOrWhiteSpace(computedSummary))
                computedSummary = conv.Summary ?? ""; // Son çare

            // --- Counters / stats ---
            var userMsgs = messages.Where(IsUserMessageDynamic).ToList();

            int totalWords = 0;
            foreach (var m in userMsgs)
                totalWords += Regex.Matches(m.Content ?? string.Empty, @"\b[\w']+\b").Count;

            // Mistake samples (CorrectedText != Content)
            var samples = new List<MistakeSampleDto>();
            foreach (var m in userMsgs)
            {
                var corrected = GetPropString(m, "CorrectedText");
                var original = m.Content ?? "";
                if (!string.IsNullOrWhiteSpace(corrected) &&
                    !string.Equals(original.Trim(), corrected.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    samples.Add(new MistakeSampleDto { Original = original, Corrected = corrected });
                }
            }
            // İstenen aralıkta kırp
            samples = samples.Take(Math.Clamp(sampleCount, 3, 5)).ToList();

            int messageCount = messages.Count;
            int userMessageCount = userMsgs.Count;
            int agentMessageCount = messageCount - userMessageCount;

            // Highlights (kullanıcı mesajlarından)
            var highlights = ExtractHighlightsInternal(
                userMsgs.Select(x => x.Content ?? string.Empty),
                Math.Clamp(highlightCount, 3, 5)
            );

            return new ConversationSummaryDto
            {
                ConversationId = conv.ConversationId,
                Summary = computedSummary,
                DurationSeconds = durationSec,
                MistakesCount = samples.Count,
                MistakeSamples = samples,
                TotalWords = totalWords,
                MessageCount = messageCount,
                UserMessageCount = userMessageCount,
                AgentMessageCount = agentMessageCount,
                StartedAtUtc = first,
                EndedAtUtc = last,
                Highlights = highlights
            };
        }
        public async Task<ConversationSummaryResult> GetConversationSummaryAsync(string conversationId)
        {
            // 1. Tüm mesajları sırayla al
            var allMessages = await _messageService.GetAllMessagesAsync(conversationId);
            if (allMessages == null || !allMessages.Any())
                return new ConversationSummaryResult();

            // 2. Mesajları bir string haline getir
            var conversationText = string.Join("\n", allMessages
                .OrderBy(m => m.CreatedAt)
                .Select(m => $"{m.Role}: {m.Content}"));

            // 3. Token dostu: uzun metinleri kır, örn. son 2000 karakter
            if (conversationText.Length > 2000)
                conversationText = conversationText.Substring(conversationText.Length - 2000);

            // 4. Kullanıcıyı Conversation tablosundan al
            var conversation = await _dbContext.Conversations.FindAsync(conversationId);
            if (conversation == null)
                return new ConversationSummaryResult();

            var user = await _userRepository.GetByIdAsync(conversation.UserId);
            var targetLanguage = user?.TargetLanguage ?? "en";

            // 5. Özetleme prompt'u
            var summaryPrompt = $@"
You are a helpful assistant. Summarize the following conversation **as a short study lesson** in a friendly way, 
Keep it **concise, maximum 15 words**, focusing on what was discussed and learned. 
Do not mention 'user' or 'assistant' labels. 
Write in {targetLanguage}.

Conversation:
{conversationText}
";

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
            new { role = "user", content = summaryPrompt }
        },
                temperature = 0.5
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            var summaryText = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? "";

            
            string title = "";

            // 6. Özet çıktı varsa title oluştur ve Conversation tablosunu güncelle
            if (!string.IsNullOrWhiteSpace(summaryText))
            {
                var titlePrompt = $@"
You are a helpful assistant. Generate a short, concise, meaningful title (5 words max) 
for the following conversation summary in {targetLanguage}. Do not add extra words.

Summary:
{summaryText}
";

                var titleRequest = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                new { role = "user", content = titlePrompt }
            },
                    temperature = 0.3
                };

                var titleResponse = await _httpClient.PostAsJsonAsync("chat/completions", titleRequest);
                titleResponse.EnsureSuccessStatusCode();

                var titleResult = await titleResponse.Content.ReadFromJsonAsync<OpenAIChatResponse>();
                title = titleResult?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? "";

                // DB'yi güncelle
                conversation.Summary = summaryText;
                conversation.Title = title;
                await _dbContext.SaveChangesAsync();
            }

            return new ConversationSummaryResult
            {
                Summary = summaryText,
                Title = title
            };
        }
        /// <summary>
        /// Konuşmadan kısa ve anlamlı bir summary üretir.
        /// Öncelik: son assistant cevabı -> ilk anlamlı (system hariç) mesaj.
        /// CorrectedText varsa onu kullanır.
        /// </summary>
        private static string ComputeSummaryFromConversation(List<Message> orderedMessages)
        {
            if (orderedMessages == null || orderedMessages.Count == 0) return "";

            // inline role helper
            bool IsAssistant(Message m)
            {
                var role = (GetPropString(m, "Role")
                            ?? GetPropString(m, "MessageRole")
                            ?? GetPropString(m, "SenderType")
                            ?? "").Trim().ToLowerInvariant();
                return role == "assistant" || role == "ai" || role == "agent";
            }

            bool IsSystem(Message m)
            {
                var role = (GetPropString(m, "Role")
                            ?? GetPropString(m, "MessageRole")
                            ?? GetPropString(m, "SenderType")
                            ?? "").Trim().ToLowerInvariant();
                return role == "system";
            }

            // 1) Son assistant cevabı
            var lastAssistant = orderedMessages
                .Where(IsAssistant)
                .OrderBy(m => m.CreatedAt)
                .LastOrDefault();

            string pickText(Message m)
            {
                var corrected = GetPropString(m, "CorrectedText");
                return string.IsNullOrWhiteSpace(corrected) ? (m.Content ?? "") : corrected;
            }

            string candidate = "";
            if (lastAssistant != null)
                candidate = pickText(lastAssistant);

            // 2) Boşsa: system olmayan ilk anlamlı mesaj
            if (string.IsNullOrWhiteSpace(candidate))
            {
                var firstMeaningful = orderedMessages
                    .Where(m => !IsSystem(m) &&
                                (!string.IsNullOrWhiteSpace(m.Content) ||
                                 !string.IsNullOrWhiteSpace(GetPropString(m, "CorrectedText"))))
                    .OrderBy(m => m.CreatedAt)
                    .FirstOrDefault();

                if (firstMeaningful != null)
                    candidate = pickText(firstMeaningful);
            }

            // Normalize + truncate
            candidate = (candidate ?? "").Trim();
            candidate = Regex.Replace(candidate, @"\s+", " ");
            const int limit = 120;
            if (candidate.Length > limit)
                candidate = candidate[..(limit - 3)] + "...";

            return candidate;
        }


        private static bool IsUserMessageDynamic(Message m)
        {
            var b = GetPropBool(m, "IsFromUser") ?? GetPropBool(m, "IsUser");
            if (b.HasValue) return b.Value;

            var s = GetPropString(m, "SenderType")
                     ?? GetPropString(m, "Sender")
                     ?? GetPropString(m, "Role")
                     ?? GetPropString(m, "Source");
            if (!string.IsNullOrEmpty(s))
                return string.Equals(s, "user", StringComparison.OrdinalIgnoreCase);

            return false;
        }

        private static bool? GetPropBool(object obj, string name)
        {
            var p = obj.GetType().GetProperty(name);
            if (p == null) return null;
            var v = p.GetValue(obj);
            if (v is bool b) return b;
            if (v is string s && bool.TryParse(s, out var bs)) return bs;
            return null;
        }

        private static string? GetPropString(object obj, string name)
        {
            var p = obj.GetType().GetProperty(name);
            return p == null ? null : p.GetValue(obj)?.ToString();
        }

        private static List<string> ExtractHighlightsInternal(IEnumerable<string> texts, int k)
        {
            var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "a", "an", "the", "and", "or", "but", "if", "then", "so", "to", "of", "in", "on", "for", "with",
                "at", "by", "from", "as", "is", "are", "was", "were", "be", "been", "am", "i", "you", "he", "she",
                "it", "we", "they", "me", "him", "her", "them", "my", "your", "our", "their", "this", "that",
                "these", "those", "there", "here", "what", "which", "who", "can", "could", "should", "would",
                "will", "just", "do", "does", "did", "done", "have", "has", "had"
            };

            var uni = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var bi = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                var matches = Regex.Matches(text.ToLowerInvariant(), @"\b[\w']+\b");
                var toks = matches.Cast<Match>().Select(m => m.Value).Where(t => !stop.Contains(t)).ToList();

                for (int i = 0; i < toks.Count; i++)
                {
                    uni[toks[i]] = uni.GetValueOrDefault(toks[i]) + 1;
                    if (i < toks.Count - 1)
                    {
                        var bg = $"{toks[i]} {toks[i + 1]}";
                        bi[bg] = bi.GetValueOrDefault(bg) + 1;
                    }
                }
            }

            return bi.Select(kv => (phrase: kv.Key, score: kv.Value * 2, len: kv.Key.Length))
                     .Concat(uni.Select(kv => (phrase: kv.Key, score: kv.Value, len: kv.Key.Length)))
                     .OrderByDescending(x => x.score)
                     .ThenByDescending(x => x.len)
                     .Select(x => x.phrase)
                     .Distinct()
                     .Take(k)
                     .ToList();
        }
        public async Task<List<Message>> GetUserMessagesWithGrammarErrorsAsync(string conversationId)
        {
            return await _dbContext.Messages
                .Where(m => m.ConversationId == conversationId
                            && m.Role == "user"
                            && m.GrammarErrors != null
                            && m.GrammarErrors.Any())
                .ToListAsync();
        }
        public async Task<List<Conversation>> SearchConversationsAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<Conversation>();

            return await _dbContext.Conversations
                .Where(c =>
                    EF.Functions.ILike(c.Title, $"%{searchText}%") ||
                    EF.Functions.ILike(c.Summary, $"%{searchText}%")
                )
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
        public async Task<List<Message>> GetUserMessagesWithGrammarErrorsByAgentAsync(string userId, string agentId)
        {
            // Önce o user-agent conversationlarını bul
            var conversationIds = await _dbContext.Conversations
                .Where(c => c.UserId == userId && c.AgentId == agentId)
                .Select(c => c.ConversationId)
                .ToListAsync();

            if (!conversationIds.Any())
                return new List<Message>();

            // Sonra user mesajlarını al, grammarErrors boş olmayanları filtrele
            var messages = await _dbContext.Messages
                .Where(m => conversationIds.Contains(m.ConversationId)
                            && m.Role == "user"
                            && m.GrammarErrors != null
                            && m.GrammarErrors.Any())
                .ToListAsync();

            return messages;
        }
        public async Task<List<Conversation>> GetConversationsByUserAndAgentAsync(string userId, string agentId)
        {
            var conversations = await _dbContext.Conversations
                .Where(c => c.UserId == userId && c.AgentId == agentId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            foreach (var conv in conversations)
            {
                if (string.IsNullOrWhiteSpace(conv.Summary) || string.IsNullOrWhiteSpace(conv.Title))
                {
                    var summaryResult = await GetConversationSummaryAsync(conv.ConversationId);
                    if (!string.IsNullOrWhiteSpace(summaryResult.Summary))
                    {
                        conv.Summary = summaryResult.Summary;
                        conv.Title = summaryResult.Title;
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }

            return conversations;
        }
    }
}