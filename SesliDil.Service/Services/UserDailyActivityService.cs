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
        public UserDailyActivityService(SesliDilDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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
    }
}
