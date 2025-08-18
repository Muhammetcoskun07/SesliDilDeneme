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
                .AnyAsync(x => x.UserId == userId
                               && x.Date.Date == today);
        }
        public async Task<UserDailyActivityDto> GetByUserAndDateAsync(string userId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var entity = await _context.UserDailyActivities
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Date >= startDate && x.Date < endDate);

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
            var dateRanges = dates.Select(d => new { Start = d.Date, End = d.Date.AddDays(1) }).ToList();

            var entities = await _context.UserDailyActivities
                .Where(x => x.UserId == userId)
                .ToListAsync(); // SQL burada biter

            // Filtreleme artık bellekte yapılır
            entities = entities
                .Where(x => dateRanges.Any(dr => x.Date >= dr.Start && x.Date < dr.End))
                .ToList();

            return _mapper.Map<IEnumerable<UserDailyActivityDto>>(entities);
        }
        public async Task<List<UserDailyActivity>> GetActivitiesByDaysAsync(string userId, List<DayOfWeek> days)
        {
            return await _context.UserDailyActivities
                .Where(x => x.UserId == userId &&
                            days.Contains(x.Date.DayOfWeek))
                .ToListAsync();
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
            if (string.IsNullOrWhiteSpace(goal))
                return 0;

            goal = goal.Trim(); // baş/son boşluk
            goal = goal.Replace('–', '-')  // en dash
                       .Replace('—', '-')  // em dash
                       .Replace('−', '-'); // minus sign

            // tüm görünmez boşluk karakterlerini normal space yap
            goal = string.Concat(goal.Select(c => char.IsWhiteSpace(c) ? ' ' : c));

            if (DailyGoalMapping.TryGetValue(goal, out int minutes))
                return minutes;

            Console.WriteLine($"[Rate] Mapping yok: '{goal}'"); // log ekle
            return 0;
        }
        public async Task<double> GetTodaySpeakingCompletionRateAsync(string userId)
        {
            Console.WriteLine($"[Rate] Başlıyor: userId = {userId}");

            // 1. User tablosundan hedefi al
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                Console.WriteLine("[Rate] Hata: Kullanıcı bulunamadı.");
                return 0;
            }

            Console.WriteLine($"[Rate] Kullanıcı bulundu: WeeklySpeakingGoal = {user.WeeklySpeakingGoal}");

            int dailyGoal = GetDailyGoalFromString(user.WeeklySpeakingGoal);
            if (dailyGoal == 0)
            {
                Console.WriteLine("[Rate] Hata: DailyGoal 0 çıktı (mapping yok).");
                return 0;
            }

            Console.WriteLine($"[Rate] DailyGoal = {dailyGoal}");

            // 2. Bugünkü aktiviteyi çek
            var today = DateTime.UtcNow.Date;
            var activity = await GetByUserAndDateAsync(userId, today);

            if (activity == null)
                Console.WriteLine("[Rate] Bugün için aktivite bulunamadı.");

            int minutesSpent = activity?.MinutesSpent ?? 0;
            Console.WriteLine($"[Rate] MinutesSpent = {minutesSpent}");

            // 3. Hesaplama
            double rate = (double)minutesSpent / dailyGoal * 100;
            rate = Math.Min(rate, 100);

            Console.WriteLine($"[Rate] Hesaplanan rate = {rate}");

            return rate;
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

        public async Task<List<UserDailyActivity>> GetActivitiesByDatesOrDaysAsync(
    string userId,
    List<DateTime>? dates = null,
    List<DayOfWeek>? days = null)
        {
            var query = _context.UserDailyActivities.AsQueryable();

            query = query.Where(x => x.UserId == userId);

            if (dates != null && dates.Any())
            {
                var dateSet = dates.Select(d => d.Date).ToHashSet();
                query = query.Where(x => dateSet.Contains(x.Date.Date));
            }

            if (days != null && days.Any())
            {
                query = query.Where(x => days.Contains(x.Date.DayOfWeek));
            }

            return await query
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }

    }
}
