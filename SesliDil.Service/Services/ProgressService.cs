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
        public string DetermineLevel(int wpm)
        {
            if (wpm <= 15) return "Beginner";
            if (wpm <= 30) return "Developing";
            if (wpm <= 50) return "Intermediate";
            if (wpm <= 80) return "Advanced";
            if (wpm <= 100) return "Fluent";
            return "Native";
        }

        // Seviye yalnızca artabilir kontrolü
        public bool IsLevelHigher(string newLevel, string currentLevel)
        {
            var levels = new[]
            {
            "Beginner",
            "Developing",
            "Intermediate",
            "Advanced",
            "Fluent",
            "Native"
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
        public string GetLevelCode(string level)
        {
            if (string.IsNullOrWhiteSpace(level))
                return null;

            return level switch
            {
                // Kısa isimler
                "Beginner" => "A1",
                "Developing" => "A2",
                "Intermediate" => "B1",
                "Advanced" => "B2",
                "Fluent" => "C1",
                "Native" => "C2",


                "A1  I know basic words and simple phrases" => "A1",
                "A2  I can carry on basic conversations" => "A2",
                "B1  I know basic words and simple phrases" => "B1",
                "B2  I can discuss various topics with ease" => "B2",
                "C1  I speak confidently in complex situations" => "C1",
                "C2  I speak like a native in all contexts" => "C2",

                _ => null
            };
        }

        public string GetNextLevelCode(string level)
        {
            // Önce mevcut seviyeyi A1–C2 koduna çevir
            var currentCode = GetLevelCode(level);

            if (string.IsNullOrEmpty(currentCode))
                return null;

            var order = new[] { "A1", "A2", "B1", "B2", "C1", "C2" };
            var index = Array.IndexOf(order, currentCode);

            if (index == -1 || index == order.Length - 1)
                return null; // son seviye ise null

            return order[index + 1];
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
