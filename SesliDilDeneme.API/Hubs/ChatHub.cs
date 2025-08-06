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
        private readonly ILogger _logger;
        private readonly ConversationService _conversationService;
        private static readonly ConcurrentDictionary<string, Stopwatch> _conversationTimers = new();

        public ChatHub(MessageService messageService, ILogger logger, ConversationService conversationService)
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

        public async Task SendMessage(SendMessageRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var aiMessage = await _messageService.SendMessageAsync(request);

                await Clients.Group(aiMessage.ConversationId).SendAsync("ReceiveMessage", aiMessage);

                stopwatch.Stop();
                _logger.LogInformation($"SendMessage completed in {stopwatch.ElapsedMilliseconds} ms for ConversationId: {aiMessage.ConversationId}");
            }
            catch (ValidationException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning($"Validation error in SendMessage: {ex.Message}, took {stopwatch.ElapsedMilliseconds} ms");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning($"Argument error in SendMessage: {ex.Message}, took {stopwatch.ElapsedMilliseconds} ms");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError($"Unexpected error in SendMessage: {ex.Message}, took {stopwatch.ElapsedMilliseconds} ms");
                await Clients.Caller.SendAsync("Error", "An error occurred while processing the message");
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
