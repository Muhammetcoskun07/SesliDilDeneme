using System.Collections.Concurrent;
using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Data.Context;
using SesliDil.Service.Services;
using SesliDilDeneme.API.Validators;

namespace SesliDilDeneme.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly MessageService _messageService;
        private readonly ILogger<ChatHub> _logger;
        private readonly ConversationService _conversationService;
        private static readonly ConcurrentDictionary<string, Stopwatch> _conversationTimers = new();
        private readonly AgentActivityService _agentActivityService;
        private readonly SesliDilDbContext _dbContext;

        public ChatHub(MessageService messageService, ILogger<ChatHub> logger, ConversationService conversationService,AgentActivityService agentActivityService,SesliDilDbContext dbContext)
        {
            _messageService = messageService;
            _logger = logger;
            _conversationService = conversationService;
            _agentActivityService = agentActivityService;
            _dbContext = dbContext;
        }
        public override async Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            var conversationId = http?.Request.Query["conversationId"].ToString();
            var userId = http?.Request.Query["userId"].ToString();
            var agentId = http?.Request.Query["agentId"].ToString();

            if (!string.IsNullOrEmpty(conversationId) && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(agentId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

                var usersDict = _agentActivityService.ActivityData.GetOrAdd(conversationId, _ => new ConcurrentDictionary<string, ConcurrentDictionary<string, AgentActivity>>());
                var agentsDict = usersDict.GetOrAdd(userId, _ => new ConcurrentDictionary<string, AgentActivity>());
                var agentActivity = agentsDict.GetOrAdd(agentId, _ => new AgentActivity());

                if (!agentActivity.Stopwatch.IsRunning)
                    agentActivity.Stopwatch.Start();

                _logger.LogInformation($"User {userId} started conversation {conversationId} with Agent {agentId} (ConnectionId: {Context.ConnectionId})");
            }
            await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var http = Context.GetHttpContext();
            var conversationId = http?.Request.Query["conversationId"].ToString();
            var userId = http?.Request.Query["userId"].ToString();
            var agentId = http?.Request.Query["agentId"].ToString();

            if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(agentId))
            {
                _logger.LogWarning($"Invalid parameters: conversationId={conversationId}, userId={userId}, agentId={agentId}");
                await base.OnDisconnectedAsync(exception);
                return;
            }

            try
            {
                var activityData = _agentActivityService.ActivityData;

                if (activityData.TryGetValue(conversationId, out var usersDict) && usersDict != null &&
                    usersDict.TryGetValue(userId, out var agentsDict) && agentsDict != null)
                {
                    if (agentsDict.TryRemove(agentId, out var agentActivity) && agentActivity != null)
                    {
                        agentActivity.Stopwatch.Stop();

                        double totalMinutes = agentActivity.Stopwatch.Elapsed.TotalMinutes;
                        double wordsPerMinute = totalMinutes > 0 ? agentActivity.WordCount / totalMinutes : 0;

                        _logger.LogInformation(
                            $"User {userId} ended conversation {conversationId} with Agent {agentId}. " +
                            $"Duration: {totalMinutes} minutes, Messages: {agentActivity.MessageCount}, " +
                            $"Words: {agentActivity.WordCount}, WPM: {wordsPerMinute}"
                        );

                        // Konuşma aktivitesini kaydet
                        await _conversationService.SaveAgentActivityAsync(
                            conversationId, userId, agentId,
                            agentActivity.Stopwatch.Elapsed,
                            agentActivity.MessageCount,
                            agentActivity.WordCount,
                            wordsPerMinute
                        );

                        // Kullanıcı seviyesini al
                        var user = await _dbContext.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u => u.UserId == userId);

                        var userLevel = user?.ProficiencyLevel ?? "A1"; // User tablosundaki seviye yoksa varsayılan A1

                        // Progress tablosu güncelle
                        var progress = await _dbContext.Progresses.FirstOrDefaultAsync(p => p.UserId == userId);

                        var now = DateTime.UtcNow;

                        if (progress != null)
                        {
                            if (wordsPerMinute > progress.BestWordsPerMinute)
                            {
                                progress.BestWordsPerMinute = wordsPerMinute;
                            }

                            var lastDate = progress.LastConversationDate.Date;
                            var today = now.Date;

                            // DailyConversationCount
                            if (lastDate == today)
                            {
                                progress.DailyConversationCount += 1;
                            }
                            else
                            {
                                progress.DailyConversationCount = 1;
                            }

                            // Toplam konuşma süresi ekle
                            progress.TotalConversationTimeMinutes += (int)Math.Round(totalMinutes);

                            // Streak hesaplamasını sonraki aşamaya bırakalım (şimdilik değiştirmiyoruz)

                            progress.LastConversationDate = now;
                            progress.CurrentLevel = userLevel;
                            progress.UpdatedAt = now;

                            await _dbContext.SaveChangesAsync();
                            _logger.LogInformation($"Progress updated for User {userId}: Level = {userLevel}, Best WPM = {wordsPerMinute}");
                        }
                        else
                        {
                            var newProgress = new Progress
                            {
                                ProgressId = Guid.NewGuid().ToString(),
                                UserId = userId,
                                BestWordsPerMinute = wordsPerMinute,
                                UpdatedAt = now,
                                LastConversationDate = now,
                                DailyConversationCount = 1,
                                TotalConversationTimeMinutes = (int)Math.Round(totalMinutes),
                                CurrentStreakDays = 0, // Henüz hesaplanmadı
                                LongestStreakDays = 0,
                                CurrentLevel = userLevel
                            };

                            await _dbContext.Progresses.AddAsync(newProgress);
                            await _dbContext.SaveChangesAsync();
                            _logger.LogInformation($"Progress created for User {userId}: Level = {userLevel}, Best WPM = {wordsPerMinute}");
                        }

                        // İstemciye gönder
                        var activityDto = new ConversationAgentActivityDto
                        {
                            ActivityId = Guid.NewGuid().ToString(),
                            ConversationId = conversationId,
                            UserId = userId,
                            AgentId = agentId,
                            DurationMinutes = totalMinutes,
                            MessageCount = agentActivity.MessageCount,
                            WordCount = agentActivity.WordCount,
                            WordsPerMinute = wordsPerMinute,
                            CreatedAt = now
                        };
                        await Clients.Caller.SendAsync("ReceiveActivityData", activityDto);
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to remove agentId {agentId} for userId {userId} in conversationId {conversationId}");
                    }

                    // Agent dictionary boşsa user'ı kaldır
                    if (agentsDict.IsEmpty)
                    {
                        if (usersDict.TryRemove(userId, out _))
                            _logger.LogInformation($"Removed userId {userId} from usersDict for conversationId {conversationId}");
                    }

                    // User dictionary boşsa conversation'ı kaldır
                    if (usersDict.IsEmpty)
                    {
                        if (activityData.TryRemove(conversationId, out _))
                            _logger.LogInformation($"Removed conversationId {conversationId} from _activityData");
                    }

                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
                }
                else
                {
                    _logger.LogWarning($"No usersDict found for conversationId {conversationId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in OnDisconnectedAsync for conversationId: {conversationId}, userId: {userId}, agentId: {agentId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
        public async Task SendMessage(string conversationId, string userId, string agentId, string content)
        {
            if (string.IsNullOrWhiteSpace(conversationId) ||
                string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(agentId) ||
                string.IsNullOrWhiteSpace(content))
            {
                await Clients.Caller.SendAsync("Error", "SendMessage: One or more required parameters are missing.");
                return;
            }

            var activityData = _agentActivityService.ActivityData;

            if (activityData.TryGetValue(conversationId, out var usersDict) &&
                usersDict.TryGetValue(userId, out var agentsDict) &&
                agentsDict.TryGetValue(agentId, out var agentActivity))
            {
                agentActivity.MessageCount++;

                // Kelime sayısını hesapla
                int wordCount = CountWords(content);
                agentActivity.WordCount += wordCount;
            }

            try
            {
                var request = new SendMessageRequest
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    AgentId = agentId,
                    Content = content
                };

                var aiMessage = await _messageService.SendMessageAsync(request);

                await Clients.Group(conversationId).SendAsync("ReceiveMessage", aiMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessage");
                await Clients.Caller.SendAsync("Error", $"Failed to send message: {ex.Message}");
            }
        }
        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            // Basit bir kelime sayımı: boşluklara göre ayır
            var words = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length;
        }
        public async Task GetConversationDuration(string conversationId)
        {
            var conversation = await _conversationService.GetByIdAsync<string>(conversationId);
            if (conversation != null && conversation.StartedAt != default)
            {
                var durationMinutes = conversation.DurationMinutes ?? (DateTime.UtcNow - conversation.StartedAt).TotalMinutes;
                await Clients.Caller.SendAsync("ConversationDuration", new
                {
                    ConversationId = conversationId,
                    DurationMinutes = durationMinutes
                });
                _logger.LogInformation($"Conversation {conversationId} duration requested: {durationMinutes} minutes");
            }
            else
            {
                await Clients.Caller.SendAsync("Error", $"No active conversation found for {conversationId}");
            }
        }

    }
}
