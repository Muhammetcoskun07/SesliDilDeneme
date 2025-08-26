using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace SesliDil.Service.Services
{
    public class CleanupService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval;

        // interval: çalıştırma aralığını ayarlamak için 
        public CleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _interval = TimeSpan.FromHours(24); 
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(async _ =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var conversationService = scope.ServiceProvider.GetRequiredService<ConversationService>();

                    // Burada 1 dakikadan kısa conversationları siliyoruz
                    int deletedCount = await conversationService.DeleteShortConversationsAsync();
                    Console.WriteLine($"[{DateTime.Now}] 1 dakikadan kısa conversationlar temizlendi. Silinen kayıt sayısı: {deletedCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata: {ex.Message}");
                }
            },
  null, TimeSpan.Zero, _interval);

            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}