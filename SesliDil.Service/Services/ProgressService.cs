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
        public async Task<ProgressDto> UpdateProgressAsync(string progressId, int dailyConversationCount, 
            int totalConversationTimeMinutes, string currentLevel)
        {
            if (string.IsNullOrEmpty(progressId)) throw new ArgumentException("Invalid progressId", nameof(progressId));
            var progress = await _progressRepository.GetByIdAsync<string>(progressId);
            progress.DailyConversationCount = dailyConversationCount;
            progress.TotalConversationTimeMinutes = totalConversationTimeMinutes;
            progress.CurrentLevel = currentLevel;
            progress.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(progress);
            return _mapper.Map<ProgressDto>(progress);
        }
    }
}
