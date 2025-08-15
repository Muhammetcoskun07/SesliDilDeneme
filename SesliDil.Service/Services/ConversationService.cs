using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;   // <-- ToListAsync(), EF.Functions, ExecuteSqlRawAsync için
using System.Linq;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Core.Mappings;
using SesliDil.Data.Context;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class ConversationService : Service<Conversation>, IService<Conversation>
    {
        private readonly IRepository<Conversation> _conversationRepository;
        private readonly IMapper _mapper;
        private readonly SesliDilDbContext _dbContext;

        public ConversationService(IRepository<Conversation> conversationRepository, IMapper mapper,SesliDilDbContext dbContext)
            : base(conversationRepository, mapper)
        {
            _dbContext = dbContext;
            _conversationRepository = conversationRepository;
            _mapper = mapper;
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
        public async Task EndConversationAsync(string conversationId, bool forceEnd = true)
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
        }
        // === SAFE ADD: Conversation'dan hesaplanmış özet (reflection ile, enum/alan bağımsız) ===
        public async Task<ConversationSummaryDto> BuildConversationSummaryComputedAsync(
            string conversationId, int sampleCount = 3, int highlightCount = 3)
        {
            if (string.IsNullOrEmpty(conversationId))
                throw new ArgumentException("Invalid Conversation Id");

            var conv = await _conversationRepository.GetByIdWithMessagesAsync(conversationId);
            if (conv == null)
                throw new Exception("Conversation not found");

            var messages = conv.Messages?.OrderBy(m => m.CreatedAt).ToList() ?? new List<Message>();

            if (messages.Count == 0)
            {
                return new ConversationSummaryDto
                {
                    ConversationId = conv.ConversationId,
                    Summary = conv.Summary ?? "",
                    DurationSeconds = 0,
                    FluencyWpm = 0,
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
            var durationSec = System.Math.Max((int)(last - first).TotalSeconds, 1);

            // SenderType / IsFromUser / Sender / Role vs. ne varsa otomatik yakalar
            var userMsgs = messages.Where(IsUserMessageDynamic).ToList();

            // Kelime sayısı & WPM
            int totalWords = 0;
            foreach (var m in userMsgs)
            {
                totalWords += System.Text.RegularExpressions.Regex
                    .Matches(m.Content ?? string.Empty, @"\b[\w']+\b").Count;
            }
            var minutes = System.Math.Max(durationSec / 60.0, 0.5);
            int wpm = (int)System.Math.Round(totalWords / minutes);

            // CorrectedText varsa örnekleri çıkar (yoksa 0 döner)
            var samples = new List<MistakeSampleDto>();
            foreach (var m in userMsgs)
            {
                var corrected = GetPropString(m, "CorrectedText");
                if (!string.IsNullOrWhiteSpace(corrected) &&
                    !string.Equals((m.Content ?? "").Trim(), corrected.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    samples.Add(new MistakeSampleDto { Original = m.Content ?? "", Corrected = corrected });
                }
            }
            samples = samples.Take(System.Math.Clamp(sampleCount, 3, 5)).ToList();

            // Count değerlerini önce al (method-group hatasını önler)
            int messageCount = messages.Count;
            int userMessageCount = userMsgs.Count;
            int agentMessageCount = messageCount - userMessageCount;

            var highlights = ExtractHighlightsInternal(
                userMsgs.Select(x => x.Content ?? string.Empty),
                System.Math.Clamp(highlightCount, 3, 5)
            );

            return new ConversationSummaryDto
            {
                ConversationId = conv.ConversationId,
                Summary = conv.Summary ?? "",
                DurationSeconds = durationSec,
                FluencyWpm = wpm,
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

        // === Helpers (alan isimleri ne olursa olsun çalışır) ===
        private static bool IsUserMessageDynamic(Message m)
        {
            // bool bayraklar
            var b = GetPropBool(m, "IsFromUser") ?? GetPropBool(m, "IsUser");
            if (b.HasValue) return b.Value;

            // string/enum temsili
            var s = GetPropString(m, "SenderType")
                 ?? GetPropString(m, "Sender")
                 ?? GetPropString(m, "Role")
                 ?? GetPropString(m, "Source");
            if (!string.IsNullOrEmpty(s))
                return string.Equals(s, "user", StringComparison.OrdinalIgnoreCase);

            // Varsayılan: user değil
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

        // === aynı dosyada zaten eklemiştim ama tekrar lazım olursa: ===
        private static List<string> ExtractHighlightsInternal(IEnumerable<string> texts, int k)
        {
            var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "a","an","the","and","or","but","if","then","so","to","of","in","on","for","with",
        "at","by","from","as","is","are","was","were","be","been","am","i","you","he","she",
        "it","we","they","me","him","her","them","my","your","our","their","this","that",
        "these","those","there","here","what","which","who","can","could","should","would",
        "will","just","do","does","did","done","have","has","had"
    };

            var uni = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var bi = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                var matches = System.Text.RegularExpressions.Regex.Matches(
                    text.ToLowerInvariant(), @"\b[\w']+\b");

                var toks = new List<string>();
                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    var tok = m.Value;
                    if (!stop.Contains(tok)) toks.Add(tok);
                }

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
            return await _dbContext.Conversations
                .Where(c => c.UserId == userId && c.AgentId == agentId)
                .OrderByDescending(c => c.CreatedAt) 
                .ToListAsync();
        }

    }
}
