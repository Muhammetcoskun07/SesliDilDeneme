using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Data.Context;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class UserDailyActivityService:IUserDailyActivityService
    {
        private readonly SesliDilDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserService _userService; 
        public UserDailyActivityService(SesliDilDbContext context, IMapper mapper, UserService userService)
        {
            _context = context;
            _mapper = mapper;
            _userService = userService;
        }
        public async Task<UserDailyActivityDto> AddAsync(UserDailyActivityDto dto)
        {
            var entity = _mapper.Map<UserDailyActivity>(dto);
            _context.UserDailyActivities.Add(entity);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserDailyActivityDto>(entity);
        }
        public async Task<IEnumerable<UserDailyActivityDto>> GetByUserIdAsync(string userId)
        {
            var entities = await _context.UserDailyActivities
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UserDailyActivityDto>>(entities);
        }
        public async Task<bool> ExistsForTodayAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.UserDailyActivities
                .AnyAsync(x => x.UserId == userId && x.Date == today);
        }
        public async Task<UserDailyActivityDto> GetByUserAndDateAsync(string userId, DateTime date)
        {
            var entity = await _context.UserDailyActivities
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Date == date);

            if (entity == null)
                return null;

            return _mapper.Map<UserDailyActivityDto>(entity);
        }

        public async Task<UserDailyActivityDto> UpdateAsync(UserDailyActivityDto dto)
        {
            var entity = await _context.UserDailyActivities.FindAsync(dto.Id);

            if (entity == null)
                throw new Exception("UserDailyActivity not found");


            await _context.SaveChangesAsync();

            return _mapper.Map<UserDailyActivityDto>(entity);
        }
        public async Task<IEnumerable<UserDailyActivityDto>> GetByUserAndDatesAsync(string userId, List<DateTime> dates)
        {
            var entities = await _context.UserDailyActivities
                .Where(x => x.UserId == userId && dates.Contains(x.Date.Date))
                .ToListAsync();

            return _mapper.Map<IEnumerable<UserDailyActivityDto>>(entities);
        }
        public async Task<double> GetTodaySpeakingCompletionRateAsync(string userId)
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return 0;

            int dailyGoal = GetDailyGoalFromString(user.WeeklySpeakingGoal); 
            if (dailyGoal == 0)
                return 0;

            var utcToday = DateTime.Today.ToUniversalTime();
            var activity = await GetByUserAndDateAsync(userId, utcToday);
            int minutesSpent = activity?.MinutesSpent ?? 0;

            double rate = (double)minutesSpent / dailyGoal * 100;
            return Math.Min(rate, 100);
        }
        private readonly Dictionary<string, int> DailyGoalMapping = new Dictionary<string, int>()
        {
            { "5-10 minutes a day", 8 },
            { "15-20 minutes a day", 18 },
            { "30 minutes a day", 30 },
            { "45+ minutes a day", 45 }
        };
        private int GetDailyGoalFromString(string goal)
        {
            if (string.IsNullOrEmpty(goal))
                return 0;

            if (DailyGoalMapping.TryGetValue(goal, out int minutes))
                return minutes;

            return 0; // Bilinmeyen hedef için 0 döner
        }
        public async Task<UserAgentStatsDto> GetUserAgentStatsAsync(string userId, string agentId)
        {
            var activities = await _context.ConversationAgentActivities
                .Where(a => a.UserId == userId && a.AgentId == agentId)
                .ToListAsync();

            if (!activities.Any())
                return new UserAgentStatsDto(); // boş dönebilir

            return new UserAgentStatsDto
            {
                TotalMinutes = activities.Sum(a => a.Duration.TotalMinutes),
                TotalMessages = activities.Sum(a => a.MessageCount),
                TotalWords = activities.Sum(a => a.WordCount),
                AverageWPM = activities.Average(a => a.WordsPerMinute)
            };
        }
        public async Task<List<UserAgentStatsDto>> GetUserAllAgentStatsAsync(string userId)
        {
            var query = await _context.ConversationAgentActivities
                .Where(a => a.UserId == userId)
                .GroupBy(a => a.AgentId)
                .Select(g => new UserAgentStatsDto
                {
                    AgentId = g.Key,
                    TotalMinutes = g.Sum(x => x.Duration.TotalMinutes),
                    TotalMessages = g.Sum(x => x.MessageCount),
                    TotalWords = g.Sum(x => x.WordCount),
                    AverageWPM = g.Average(x => x.WordsPerMinute)
                })
                .ToListAsync();

            return query;
        }



    }
}
