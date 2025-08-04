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

        public ChatHub(MessageService messageService)
        {
            _messageService = messageService;
        }

        public async Task SendMessage(SendMessageRequest request)
        {
            try
            {
                // MessageService ile mesajı işle
                var message = await _messageService.SendMessageAsync(request);

                // Mesajı ilgili konuşmadaki tüm istemcilere gönder
                await Clients.Group(message.ConversationId)
                    .SendAsync("ReceiveMessage", message);
            }
            catch (ValidationException ex)
            {
                // FluentValidation hatalarını istemciye gönder
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            catch (ArgumentException ex)
            {
                // Diğer hata mesajlarını istemciye gönder
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            catch (Exception ex)
            {
                // Loglama yapılabilir
                await Clients.Caller.SendAsync("Error", "An error occurred while processing the message");
            }
        }
    }
}
