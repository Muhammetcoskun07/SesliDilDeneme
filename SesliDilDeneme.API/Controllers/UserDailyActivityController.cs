using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Service.Interfaces;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserDailyActivityController:ControllerBase
    {
        private readonly IUserDailyActivityService _service;

        public UserDailyActivityController(IUserDailyActivityService service)
        {
            _service = service;
        }
        
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetByUser(string userId)
        {
            var result = await _service.GetByUserIdAsync(userId);
            return Ok(result);
        }

        [HttpGet("{userId}/exists-today")]
        public async Task<IActionResult> ExistsForToday(string userId)
        {
            var exists = await _service.ExistsForTodayAsync(userId);
            return Ok(new { exists });
        }
        [HttpPost("check-activities")]
        public async Task<IActionResult> CheckActivities([FromBody] UserDailyActivityCheckRequest request)
        {
            if (request == null || request.Dates == null || !request.Dates.Any())
                return BadRequest("Dates list cannot be empty.");

            var activities = await _service.GetByUserAndDatesAsync(request.UserId, request.Dates);

            // Aktivite olan tarihleri döndür
            var completedDates = activities.Select(a => a.Date.Date).ToList();

            return Ok(new
            {
                UserId = request.UserId,
                CompletedDates = completedDates
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
