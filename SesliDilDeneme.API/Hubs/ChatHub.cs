using System.Collections.Concurrent;
using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Data.Context;
using SesliDil.Service.Interfaces;
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
        private readonly IUserDailyActivityService _userDailyActivityService;

        public ChatHub(MessageService messageService, ILogger<ChatHub> logger, ConversationService conversationService,AgentActivityService agentActivityService,SesliDilDbContext dbContext,IUserDailyActivityService userDailyActivityService)
        {
            _messageService = messageService;
            _logger = logger;
            _conversationService = conversationService;
            _agentActivityService = agentActivityService;
            _dbContext = dbContext;
            _userDailyActivityService = userDailyActivityService;
        }
        public override async Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            var conversationId = http?.Request.Query["conversationId"].ToString();
            var userId = http?.Request.Query["userId"].ToString();
            var agentId = http?.Request.Query["agentId"].ToString();

            if (!string.IsNullOrEmpty(conversationId) && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(agentId))
            {
                // Gruba ekle
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

                // Aktivite servisi güncelle
                var usersDict = _agentActivityService.ActivityData
                    .GetOrAdd(conversationId, _ => new ConcurrentDictionary<string, ConcurrentDictionary<string, AgentActivity>>());

                var agentsDict = usersDict
                    .GetOrAdd(userId, _ => new ConcurrentDictionary<string, AgentActivity>());

                var agentActivity = agentsDict.GetOrAdd(agentId, _ => new AgentActivity());

                if (!agentActivity.Stopwatch.IsRunning)
                    agentActivity.Stopwatch.Start();

                _logger.LogInformation($"User {userId} joined conversation {conversationId} with Agent {agentId}");

                await Clients.Caller.SendAsync("Connected", new
                {
                    Message = "Bağlantı başarılı",
                    ConversationId = conversationId,
                    UserId = userId,
                    AgentId = agentId
                });
            }
            else
            {
                _logger.LogWarning("OnConnectedAsync: Eksik parametre.");
                await Clients.Caller.SendAsync("Error", "Eksik parametreler.");
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
                            var yesterday = today.AddDays(-1);
                            var existingActivity = await _userDailyActivityService.GetByUserAndDateAsync(userId, today);

                            if (existingActivity != null)
                            {
                                existingActivity.MinutesSpent += (int)Math.Round(totalMinutes);
                                await _userDailyActivityService.UpdateAsync(existingActivity);
                            }
                            else
                            {
                                var newActivity = new UserDailyActivityDto
                                {
                                    Id = Guid.NewGuid().ToString(), // ID atandı
                                    UserId = userId,
                                    Date = today,
                                    MinutesSpent = (int)Math.Round(totalMinutes)
                                };
                                await _userDailyActivityService.AddAsync(newActivity);
                            }

                            // DailyConversationCount
                            if (lastDate == today)
                            {
                                progress.DailyConversationCount += 1;
                            }
                            else
                            {
                                progress.DailyConversationCount = 1;
                            }

                            // Streak hesaplama
                            if (lastDate == yesterday)
                            {
                                progress.CurrentStreakDays += 1;
                            }
                            else if (lastDate == today)
                            {
                                // Aynı gün içinde tekrar ise streak değişmez
                            }
                            else
                            {
                                progress.CurrentStreakDays = 1;
                            }

                            // En uzun streak güncelle
                            if (progress.CurrentStreakDays > progress.LongestStreakDays)
                            {
                                progress.LongestStreakDays = progress.CurrentStreakDays;
                            }

                            // Toplam konuşma süresi ekle
                            progress.TotalConversationTimeMinutes += (int)Math.Round(totalMinutes);

                            progress.LastConversationDate = now;
                            progress.CurrentLevel = userLevel;
                            progress.UpdatedAt = now;

                            await _dbContext.SaveChangesAsync();

                            _logger.LogInformation($"Progress updated for User {userId}: Level = {userLevel}, Best WPM = {wordsPerMinute}, Current Streak = {progress.CurrentStreakDays}");
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
                                CurrentStreakDays = 1, // Yeni kayıt için başlangıç 1
                                LongestStreakDays = 1,
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


                    // User dictionary boşsa conversation'ı kaldır

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
            _logger.LogInformation($"SendMessage: conversationId={conversationId}, userId={userId}, agentId={agentId}, content={content}");

            // Validate input parameters
            if (string.IsNullOrWhiteSpace(conversationId) ||
                string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(agentId) ||
                string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("SendMessage: One or more required parameters are missing.");
                await Clients.Caller.SendAsync("Error", "Missing or invalid parameters.");
                return;
            }

            try
            {
                // Check and initialize activity data
                var activityData = _agentActivityService.ActivityData;
                var usersDict = activityData.GetOrAdd(conversationId, _ => new ConcurrentDictionary<string, ConcurrentDictionary<string, AgentActivity>>());
                var agentsDict = usersDict.GetOrAdd(userId, _ => new ConcurrentDictionary<string, AgentActivity>());
                var agentActivity = agentsDict.GetOrAdd(agentId, _ => new AgentActivity());

                // Ensure stopwatch is running
                if (!agentActivity.Stopwatch.IsRunning)
                {
                    agentActivity.Stopwatch.Start();
                    _logger.LogInformation($"Started stopwatch for agentActivity: conversationId={conversationId}, userId={userId}, agentId={agentId}");
                }

                // Update message and word counts
                agentActivity.MessageCount++;
                int wordCount = CountWords(content);
                agentActivity.WordCount += wordCount;
                _logger.LogInformation($"Updated agentActivity: MessageCount={agentActivity.MessageCount}, WordCount={agentActivity.WordCount}");

                // 1️⃣ Add user message to DB
                //var userMessage = new Message
                //{
                //    MessageId = Guid.NewGuid().ToString(),
                //    ConversationId = conversationId,
                //    Role = "user",
                //    SpeakerType = "user",
                //    Content = content,
                //    CreatedAt = DateTime.UtcNow
                //};
                //await _dbContext.Messages.AddAsync(userMessage);
                //await _dbContext.SaveChangesAsync();
                //_logger.LogInformation($"Saved user message: MessageId={userMessage.MessageId}");

                // 2️⃣ Get AI response
                var request = new SendMessageRequest
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    AgentId = agentId,
                    Content = content
                };
                _logger.LogInformation($"Calling SendMessageAsync: conversationId={conversationId}, userId={userId}, agentId={agentId}");

                var aiMessage = await _messageService.SendMessageAsync(request);
                if (aiMessage == null || string.IsNullOrWhiteSpace(aiMessage.Content))
                {
                    _logger.LogError("SendMessageAsync returned null or empty response.");
                    await Clients.Caller.SendAsync("Error", "Failed to generate AI response.");
                    return;
                }
                _logger.LogInformation($"Received AI response: Content={aiMessage.Content}");

                // 3️⃣ Add AI message to DB
                //var aiDbMessage = new Message
                //{
                //    MessageId = Guid.NewGuid().ToString(),
                //    ConversationId = conversationId,
                //    Role = "ai",
                //    SpeakerType = "ai",
                //    Content = aiMessage.Content,
                //    TranslatedContent = aiMessage.TranslatedContent,
                //    AudioUrl = aiMessage.AudioUrl,
                //    GrammarErrors = aiMessage.GrammarErrors,
                //    CreatedAt = DateTime.UtcNow
                //};
                //await _dbContext.Messages.AddAsync(aiDbMessage);
                //await _dbContext.SaveChangesAsync();
                //_logger.LogInformation($"Saved AI message: MessageId={aiDbMessage.MessageId}");

                // 4️⃣ Send to clients
                await Clients.Group(conversationId).SendAsync("ReceiveMessage", aiMessage);
                _logger.LogInformation($"Sent AI message to group: conversationId={conversationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error in SendMessage: conversationId={conversationId}, userId={userId}, agentId={agentId}");
                await Clients.Caller.SendAsync("Error", $"Unexpected error: {ex.Message}");
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
