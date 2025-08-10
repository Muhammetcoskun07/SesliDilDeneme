using System.Collections.Concurrent;
using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
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

        public ChatHub(MessageService messageService, ILogger<ChatHub> logger, ConversationService conversationService,AgentActivityService agentActivityService)
        {
            _messageService = messageService;
            _logger = logger;
            _conversationService = conversationService;
            _agentActivityService = agentActivityService;
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
                var activityData = _agentActivityService.ActivityData; // Buradan alıyoruz artık

                if (activityData.TryGetValue(conversationId, out var usersDict) && usersDict != null)
                {
                    if (usersDict.TryGetValue(userId, out var agentsDict) && agentsDict != null)
                    {
                        if (agentsDict.TryRemove(agentId, out var agentActivity) && agentActivity != null)
                        {
                            agentActivity.Stopwatch.Stop();
                            _logger.LogInformation($"User {userId} ended conversation {conversationId} with Agent {agentId}. Duration: {agentActivity.Stopwatch.Elapsed.TotalMinutes} minutes, Messages: {agentActivity.MessageCount}");

                            await _conversationService.SaveAgentActivityAsync(conversationId, userId, agentId, agentActivity.Stopwatch.Elapsed, agentActivity.MessageCount);

                            var activityDto = new ConversationAgentActivityDto
                            {
                                ActivityId = Guid.NewGuid().ToString(),
                                ConversationId = conversationId,
                                UserId = userId,
                                AgentId = agentId,
                                DurationMinutes = agentActivity.Stopwatch.Elapsed.TotalMinutes,
                                MessageCount = agentActivity.MessageCount,
                                CreatedAt = DateTime.UtcNow
                            };
                            await Clients.Caller.SendAsync("ReceiveActivityData", activityDto);
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to remove agentId {agentId} for userId {userId} in conversationId {conversationId}");
                        }

                        if (agentsDict.IsEmpty)
                        {
                            if (usersDict.TryRemove(userId, out _))
                            {
                                _logger.LogInformation($"Removed userId {userId} from usersDict for conversationId {conversationId}");
                            }
                            else
                            {
                                _logger.LogWarning($"Failed to remove userId {userId} from usersDict for conversationId {conversationId}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No agentsDict found for userId {userId} in conversationId {conversationId}");
                    }

                    if (usersDict.IsEmpty)
                    {
                        if (activityData.TryRemove(conversationId, out _))
                        {
                            _logger.LogInformation($"Removed conversationId {conversationId} from _activityData");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to remove conversationId {conversationId} from _activityData");
                        }
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
