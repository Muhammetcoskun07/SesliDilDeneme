using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Data.Context;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class UserDailyActivityService : IUserDailyActivityService
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
                .AnyAsync(x => x.UserId == userId && x.Date.Date == today);
        }

        public async Task<UserDailyActivityDto> GetByUserAndDateAsync(string userId, DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            var entity = await _context.UserDailyActivities
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Date >= start && x.Date < end);

            return entity == null ? null : _mapper.Map<UserDailyActivityDto>(entity);
        }

        public async Task<IEnumerable<UserDailyActivityDto>> GetByUserAndDatesAsync(string userId, List<DateTime> dates)
        {
            var entities = await _context.UserDailyActivities
                .Where(x => x.UserId == userId && dates.Contains(x.Date.Date))
                .ToListAsync();

            return _mapper.Map<IEnumerable<UserDailyActivityDto>>(entities);
        }

        public async Task<List<UserDailyActivity>> GetActivitiesByDaysAsync(string userId, List<DayOfWeek> days)
        {
            return await _context.UserDailyActivities
                .Where(x => x.UserId == userId && days.Contains(x.Date.DayOfWeek))
                .ToListAsync();
        }
    }
}
