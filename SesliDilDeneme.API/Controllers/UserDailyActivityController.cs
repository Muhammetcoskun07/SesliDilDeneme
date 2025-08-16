using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetByUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "Invalid userId.", error = "UserId is required.", data = (object?)null });

            var result = await _service.GetByUserIdAsync(userId);
            return Ok(new { message = "Daily activities fetched successfully.", error = (string?)null, data = result });
        }

        [HttpGet("{userId}/exists-today")]
        public async Task<IActionResult> ExistsForToday(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "Invalid userId.", error = "UserId is required.", data = (object?)null });

            var exists = await _service.ExistsForTodayAsync(userId);
            return Ok(new { message = "Existence check completed.", error = (string?)null, data = new { exists } });
        }

        [HttpGet("check-weekly-activities")]
        public async Task<IActionResult> CheckWeeklyActivities([FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "Invalid request.", error = "UserId is required.", data = (object?)null });

            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-6); // son 7 gün

            var dateList = Enumerable.Range(0, 7)
                                      .Select(i => startDate.AddDays(i))
                                      .ToList();

            var activities = await _service.GetByUserAndDatesAsync(userId, dateList);
            var completedDates = activities
                .Select(a => a.Date.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return Ok(new
            {
                message = "Weekly activities fetched successfully.",
                error = (string?)null,
                data = new
                {
                    userId,
                    completedDates
                }
            });
        }
        [HttpGet("weekly-activities/{userId}")]
        public async Task<IActionResult> GetWeeklyActivities(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "Invalid request.", error = "UserId is required.", data = (object?)null });

            // Bugün hangi gün ise haftanın Pazartesi'si ile başla
            var today = DateTime.UtcNow.Date;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = today.AddDays(-diff); // Pazartesi
            var weekDays = Enumerable.Range(0, 7)
                                     .Select(i => weekStart.AddDays(i))
                                     .ToList();

            // Veritabanından haftalık aktiviteleri al
            var activities = await _service.GetByUserAndDatesAsync(userId, weekDays);

            // Pazartesi-Sunday şeklinde günlük doluluk durumu
            var weeklyReport = weekDays.Select(day => new
            {
                Day = day.DayOfWeek.ToString(),
                Date = day,
                HasActivity = activities.Any(a => a.Date.Date == day)
            }).ToList();

            return Ok(new
            {
                message = "Weekly activities retrieved successfully.",
                error = (string?)null,
                data = new
                {
                    userId,
                    weeklyReport
                }
            });
        }
        [HttpGet("{userId}/today-speaking-completion")]
        public async Task<IActionResult> GetTodaySpeakingCompletion(string userId)
        {
            var completionRate = await _service.GetTodaySpeakingCompletionRateAsync(userId);
            return Ok(new { CompletionRate = completionRate });
        }
    }


}
