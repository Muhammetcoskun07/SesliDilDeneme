using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProgressController : Controller
    {
        private readonly ProgressService _progressService;
        public ProgressController(ProgressService progressService)
        {
            _progressService = progressService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var progresses = await _progressService.GetAllAsync();
            return Ok(new { message = "Progress list fetched successfully.", error = (string?)null, data = progresses });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid id.", error = "ID is required.", data = (object?)null });

            var progress = await _progressService.GetByIdAsync<string>(id);
            if (progress == null)
                return NotFound(new { message = "Progress not found.", error = "NOT_FOUND", data = (object?)null });

            return Ok(new { message = "Progress fetched successfully.", error = (string?)null, data = progress });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { message = "Invalid userId.", error = "UserId is required.", data = (object?)null });

            var progresses = await _progressService.GetByUserIdAsync(userId);
            return Ok(new { message = "Progress list for user fetched successfully.", error = (string?)null, data = progresses });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProgressDto progressDto)
        {
            if (progressDto == null || string.IsNullOrEmpty(progressDto.UserId))
                return BadRequest(new { message = "Invalid progress data.", error = "UserId is required.", data = (object?)null });

            var progress = new SesliDil.Core.Entities.Progress
            {
                ProgressId = Guid.NewGuid().ToString(),
                UserId = progressDto.UserId,
                DailyConversationCount = progressDto.DailyConversationCount,
                TotalConversationTimeMinutes = progressDto.TotalConversationTimeMinutes,
                CurrentStreakDays = progressDto.CurrentStreakDays,
                LongestStreakDays = progressDto.LongestStreakDays,
                CurrentLevel = progressDto.CurrentLevel,
                LastConversationDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _progressService.CreateAsync(progress);

            return CreatedAtAction(nameof(GetById), new { id = progress.ProgressId }, new
            {
                message = "Progress created successfully.",
                error = (string?)null,
                data = progress
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ProgressDto progressDto)
        {
            if (string.IsNullOrEmpty(id) || progressDto == null)
                return BadRequest(new { message = "Invalid input.", error = "ID and body are required.", data = (object?)null });

            var progress = await _progressService.GetByIdAsync<string>(id);
            if (progress == null)
                return NotFound(new { message = "Progress not found.", error = "NOT_FOUND", data = (object?)null });

            progress.UserId = progressDto.UserId;
            progress.DailyConversationCount = progressDto.DailyConversationCount;
            progress.TotalConversationTimeMinutes = progressDto.TotalConversationTimeMinutes;
            progress.CurrentStreakDays = progressDto.CurrentStreakDays;
            progress.LongestStreakDays = progressDto.LongestStreakDays;
            progress.CurrentLevel = progressDto.CurrentLevel;
            progress.LastConversationDate = progressDto.LastConversationDate;
            progress.UpdatedAt = DateTime.UtcNow;

            await _progressService.UpdateAsync(progress);

            return Ok(new { message = "Progress updated successfully.", error = (string?)null, data = progress });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid id.", error = "ID is required.", data = (object?)null });

            var progress = await _progressService.GetByIdAsync<string>(id);
            if (progress == null)
                return NotFound(new { message = "Progress not found.", error = "NOT_FOUND", data = (object?)null });

            await _progressService.DeleteAsync(progress);

            return Ok(new { message = "Progress deleted successfully.", error = (string?)null, data = (object?)null });
        }
    }
}
