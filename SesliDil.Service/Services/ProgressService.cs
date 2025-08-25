using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class ProgressService : Service<Progress>,IService<Progress>
    {
        private readonly IRepository<Progress> _progressRepository;
        private readonly IMapper _mapper;
        public ProgressService(IRepository<Progress> progressRepository, IMapper mapper) : base(progressRepository, mapper) 
        {
            _progressRepository = progressRepository;
            _mapper = mapper;
        }
        public async Task<IEnumerable<ProgressDto>> GetByUserIdAsync(string userId)
        {
            if(string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException("Invalid UserId",nameof(userId));
            var progresses=await _progressRepository.GetAllAsync();
            var userProgresses=progresses.Where(p => p.UserId == userId);
            return _mapper.Map<IEnumerable<ProgressDto>>(userProgresses);
        }
        private string DetermineLevel(int wpm)
        {
            if (wpm <= 15) return "A1  I know basic words and simple phrases";
            if (wpm <= 30) return "A2  I can carry on basic conversations";
            if (wpm <= 50) return "B1  I know basic words and simple phrases";
            if (wpm <= 80) return "B2  I can discuss various topics with ease";
            if (wpm <= 100) return "C1  I speak confidently in complex situations";
            return "C2  I speak like a native in all contexts";
        }

        // Seviye yalnızca artabilir kontrolü
        private bool IsLevelHigher(string newLevel, string currentLevel)
        {
            var levels = new[]
            {
            "A1  I know basic words and simple phrases",
            "A2  I can carry on basic conversations",
            "B1  I know basic words and simple phrases",
            "B2  I can discuss various topics with ease",
            "C1  I speak confidently in complex situations",
            "C2  I speak like a native in all contexts"
        };

            return Array.IndexOf(levels, newLevel) > Array.IndexOf(levels, currentLevel);
        }
        public async Task<ProgressDto> UpdateProgressAsync(string userId, int conversationTimeMinutes)
        {
            if (string.IsNullOrWhiteSpace(userId) || conversationTimeMinutes < 0)
                throw new ArgumentException("Invalid input");

            var allProgress = await _progressRepository.GetAllAsync();
            var progress = allProgress.FirstOrDefault(p => p.UserId == userId)
                ?? new Progress
                {
                    ProgressId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    UpdatedAt = DateTime.UtcNow
                };

            progress.DailyConversationCount += 1;
            progress.TotalConversationTimeMinutes += conversationTimeMinutes;

            // Yeni seviye DB'deki BestWordsPerMinute üzerinden
            string newLevel = DetermineLevel((int)progress.BestWordsPerMinute);

            // Seviye yalnızca artabilir
            if (IsLevelHigher(newLevel, progress.CurrentLevel))
                progress.CurrentLevel = newLevel;

            // Streak güncelleme
            var previousDate = progress.LastConversationDate.Date;
            var currentDate = DateTime.UtcNow.Date;

            if (previousDate == currentDate.AddDays(-1))
                progress.CurrentStreakDays += 1;
            else if (previousDate < currentDate.AddDays(-1))
                progress.CurrentStreakDays = 1;
            else if (previousDate != currentDate)
                progress.CurrentStreakDays = 1;

            progress.LastConversationDate = DateTime.UtcNow;
            progress.UpdatedAt = DateTime.UtcNow;

            if (progress.CurrentStreakDays > progress.LongestStreakDays)
                progress.LongestStreakDays = progress.CurrentStreakDays;

            _progressRepository.Update(progress);
            await _progressRepository.SaveChangesAsync();

            return _mapper.Map<ProgressDto>(progress);
        }

        public async Task<Progress> GetSingleByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException("Invalid UserId", nameof(userId));

            var progresses = await _progressRepository.GetAllAsync();
            var userProgress = progresses.FirstOrDefault(p => p.UserId == userId);
            return userProgress;
        }
        public async Task<Progress> CreateProgressAsync(ProgressDto progressDto)
        {
            if (progressDto is null || string.IsNullOrWhiteSpace(progressDto.UserId))
                throw new ArgumentException("Geçersiz ilerleme verisi.");

            var progress = new Progress
            {
                ProgressId = Guid.NewGuid().ToString(),
                UserId = progressDto.UserId,
                DailyConversationCount = progressDto.DailyConversationCount,
                TotalConversationTimeMinutes = progressDto.TotalConversationTimeMinutes,
                CurrentStreakDays = progressDto.CurrentStreakDays,
                LongestStreakDays = progressDto.LongestStreakDays,
                CurrentLevel = progressDto.CurrentLevel,
                LastConversationDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await CreateAsync(progress);
            return progress;
        }
        public async Task<Progress> UpdateProgressAsync(string id, ProgressDto progressDto)
        {
            if (string.IsNullOrWhiteSpace(id) || progressDto is null)
                throw new ArgumentException("Geçersiz giriş.");

            var progress = await GetByIdAsync<string>(id);
            if (progress is null)
                throw new KeyNotFoundException();

            progress.UserId = progressDto.UserId;
            progress.DailyConversationCount = progressDto.DailyConversationCount;
            progress.TotalConversationTimeMinutes = progressDto.TotalConversationTimeMinutes;
            progress.CurrentStreakDays = progressDto.CurrentStreakDays;
            progress.LongestStreakDays = progressDto.LongestStreakDays;
            progress.CurrentLevel = progressDto.CurrentLevel;
            progress.LastConversationDate = progressDto.LastConversationDate == default
                ? progress.LastConversationDate
                : progressDto.LastConversationDate;
            progress.UpdatedAt = DateTime.UtcNow;

            await UpdateAsync(progress);
            return progress;
        }
        public async Task DeleteProgressAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var progress = await GetByIdAsync<string>(id);
            if (progress is null)
                throw new KeyNotFoundException();

            await DeleteAsync(progress);
        }
      
    }
}
