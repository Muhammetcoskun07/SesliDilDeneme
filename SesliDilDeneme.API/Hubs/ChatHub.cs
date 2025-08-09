using System.Collections.Concurrent;
using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SesliDil.Core.DTOs;
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

        public ChatHub(MessageService messageService, ILogger<ChatHub> logger, ConversationService conversationService)
        {
            _messageService = messageService;
            _logger = logger;
            _conversationService = conversationService;
        }

        public override async Task OnConnectedAsync()
        {
            var conversationId = Context.GetHttpContext()?.Request.Query["conversationId"].ToString();
            if (!string.IsNullOrEmpty(conversationId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
                _conversationTimers.TryAdd(conversationId, Stopwatch.StartNew());
                _logger.LogInformation($"Conversation {conversationId} started for ConnectionId: {Context.ConnectionId}");

                var conversation = await _conversationService.GetByIdAsync<string>(conversationId);
                if (conversation == null)
                {
                    _logger.LogWarning($"Conversation {conversationId} not found on connect");
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var conversationId = Context.GetHttpContext()?.Request.Query["conversationId"].ToString();
            if (!string.IsNullOrEmpty(conversationId) && _conversationTimers.TryRemove(conversationId, out var stopwatch))
            {
                stopwatch.Stop();
                var durationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogInformation($"Conversation {conversationId} ended. Duration: {durationMs} ms");

                try
                {
                    await _conversationService.EndConversationAsync(conversationId);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError($"Error ending conversation {conversationId}: {ex.Message}");
                }

                var conversation = await _conversationService.GetByIdAsync<string>(conversationId);
                if (conversation != null && conversation.StartedAt != default)
                {
                    await Clients.Group(conversationId).SendAsync("ConversationEnded", new
                    {
                        ConversationId = conversationId,
                        DurationMinutes = conversation.DurationMinutes ?? (DateTime.UtcNow - conversation.StartedAt).TotalMinutes
                    });
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
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
