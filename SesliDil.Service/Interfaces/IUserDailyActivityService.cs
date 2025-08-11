using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SesliDil.Core.DTOs;

namespace SesliDil.Service.Interfaces
{
    public interface IUserDailyActivityService
    {
        Task<UserDailyActivityDto> AddAsync(UserDailyActivityDto dto);
        Task<IEnumerable<UserDailyActivityDto>> GetByUserIdAsync(string userId);
        Task<bool> ExistsForTodayAsync(string userId);
        Task<UserDailyActivityDto> GetByUserAndDateAsync(string userId, DateTime date);
        Task<UserDailyActivityDto> UpdateAsync(UserDailyActivityDto dto);
        Task<IEnumerable<UserDailyActivityDto>> GetByUserAndDatesAsync(string userId, List<DateTime> dates);
        Task<double> GetTodaySpeakingCompletionRateAsync(string userId);

    }
}
