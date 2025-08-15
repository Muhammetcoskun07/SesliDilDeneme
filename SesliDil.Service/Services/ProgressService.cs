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
        public async Task<ProgressDto> UpdateProgressAsync(string userId, int conversationTimeMinutes)
        {
            if (string.IsNullOrWhiteSpace(userId) || conversationTimeMinutes < 0)
                throw new ArgumentException("Invalid input");

            // userId ile Progress nesnesini manuel sorgulama
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

            progress.CurrentLevel = DetermineLevel((int)progress.BestWordsPerMinute);

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

        private string DetermineLevel(int wpm)
        {
            if (wpm <= 60) return "A1  I know basic words and simple phrases";
            if (wpm <= 80) return "A2  I can carry on basic conversations";
            if (wpm <= 100) return "B1  I know basic words and simple phrases";
            if (wpm <= 120) return "B2  I can discuss various topics with ease";
            if (wpm <= 140) return "C1  I speak confidently in complex situations";
            return "C2  I speak like a native in all contexts";
        }
    }
}
