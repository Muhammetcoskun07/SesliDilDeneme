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

        public async Task SendMessage(MessageDto message,string agentId)
        {
            // Doğrulama
            var validator = new MessageValidator();
            var result = validator.Validate(message);
            if (!result.IsValid)
            {
                await Clients.Caller.SendAsync("ReceiveError", result.Errors.Select(e => e.ErrorMessage));
                return;
            }
            message.GrammarErrors = new List<string>();

            // Ses dosyası varsa, texte çevir ve çevir
            if (!string.IsNullOrEmpty(message.AudioUrl))
            {
                message.Content = await _messageService.ConvertSpeechToTextAsync(message.AudioUrl);
                var targetLanguage = "Spanish"; // Mock: UserDto’dan alınabilir
                message.TranslatedContent = await _messageService.TranslateAsync(message.Content, targetLanguage, agentId);
                message.GrammarErrors = await _messageService.CheckGrammarAsync(message.Content,agentId);
            }

            // Mesajı tüm istemcilere gönder
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
