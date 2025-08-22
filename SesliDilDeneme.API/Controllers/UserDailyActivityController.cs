using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Service.Interfaces;

namespace SesliDilDeneme.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserDailyActivityController : ControllerBase
    {
        private readonly IUserDailyActivityService _service;

        public UserDailyActivityController(IUserDailyActivityService service)
        {
            _service = service;
        }

        // GET: api/UserDailyActivity/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetByUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId zorunludur.");

            var result = await _service.GetByUserIdAsync(userId);
            return Ok(result);
        }

        // GET: api/UserDailyActivity/{userId}/exists-today
        [HttpGet("{userId}/exists-today")]
        public async Task<IActionResult> ExistsForToday(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId zorunludur.");

            var exists = await _service.ExistsForTodayAsync(userId);
            return Ok(new { exists });
        }

        // GET: api/UserDailyActivity/check-weekly-activities?userId=...
        [HttpGet("check-weekly-activities")]
        public async Task<IActionResult> CheckWeeklyActivities([FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId zorunludur.");

            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-6); // son 7 gün
            var dateList = Enumerable.Range(0, 7).Select(i => startDate.AddDays(i)).ToList();

            var activities = await _service.GetByUserAndDatesAsync(userId, dateList);
            var completedDates = activities
                .Select(a => a.Date.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return Ok(new { userId, completedDates });
        }

        // GET: api/UserDailyActivity/weekly-activities/{userId}
        [HttpGet("weekly-activities/{userId}")]
        public async Task<IActionResult> GetWeeklyActivities(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId zorunludur.");

            var today = DateTime.UtcNow.Date;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = today.AddDays(-diff); // Pazartesi
            var weekDays = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList();

            var activities = await _service.GetByUserAndDatesAsync(userId, weekDays);

            var weeklyReport = weekDays.Select(day => new
            {
                Day = day.DayOfWeek.ToString(),
                Date = day,
                HasActivity = activities.Any(a => a.Date.Date == day)
            }).ToList();

            return Ok(new { userId, weeklyReport });
        }

        // GET: api/UserDailyActivity/{userId}/today-speaking-completion
        [HttpGet("{userId}/today-speaking-completion")]
        public async Task<IActionResult> GetTodaySpeakingCompletion(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId zorunludur.");

            var completionRate = await _service.GetTodaySpeakingCompletionRateAsync(userId);
            return Ok(new { userId, completionRate });
        }
    }
}
