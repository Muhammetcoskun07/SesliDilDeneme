using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
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

        [HttpPost("check-activities")]
        public async Task<IActionResult> CheckActivities([FromBody] UserDailyActivityCheckRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserId))
                return BadRequest(new { message = "Invalid request.", error = "UserId is required.", data = (object?)null });

            if (request.Dates == null || !request.Dates.Any())
                return BadRequest(new { message = "Invalid request.", error = "Dates list cannot be empty.", data = (object?)null });

            var activities = await _service.GetByUserAndDatesAsync(request.UserId, request.Dates);
            var completedDates = activities.Select(a => a.Date.Date).ToList();

            return Ok(new
            {
                message = "Activities checked successfully.",
                error = (string?)null,
                data = new
                {
                    userId = request.UserId,
                    completedDates
                }
            });
        }

        [HttpGet("{userId}/today-speaking-completion")]
        public async Task<IActionResult> GetTodaySpeakingCompletion(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "Invalid userId.", error = "UserId is required.", data = (object?)null });

            var completionRate = await _service.GetTodaySpeakingCompletionRateAsync(userId);
            return Ok(new { message = "Today speaking completion fetched.", error = (string?)null, data = new { completionRate } });
        }
    }
}
