using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SesliDil.Service.Services
{
    public class AudioCleanupService : BackgroundService
    {
        private readonly ILogger<AudioCleanupService> _logger;
        private readonly string _folderPath;
        private readonly TimeSpan _maxFileAge = TimeSpan.FromDays(1); //1 günden eski dosyalar silinecek
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(8); // Her 8 saatte bir çalışacak
        public AudioCleanupService(ILogger<AudioCleanupService> logger)
        {
            _logger = logger;
            _folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audio");
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AudioCleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanOldAudioFiles();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during audio cleanup.");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("AudioCleanupService stopped.");
        }
        private void CleanOldAudioFiles()
        {
            if (!Directory.Exists(_folderPath))
            {
                _logger.LogWarning("Audio folder not found: {FolderPath}", _folderPath);
                return;
            }

            var files = Directory.GetFiles(_folderPath, "*.mp3");

            int deletedFilesCount = 0;
            foreach (var file in files)
            {
                var creationTime = File.GetCreationTimeUtc(file);
                if (DateTime.UtcNow - creationTime > _maxFileAge)
                {
                    try
                    {
                        File.Delete(file);
                        deletedFilesCount++;
                        _logger.LogInformation("Deleted old audio file: {FileName}", Path.GetFileName(file));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete audio file: {FileName}", Path.GetFileName(file));
                    }
                }
            }

            _logger.LogInformation("Audio cleanup completed. Deleted {Count} files.", deletedFilesCount);
        }

    }
}
