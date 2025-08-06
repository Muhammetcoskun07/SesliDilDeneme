using System;
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
        public async Task<ActionResult<IEnumerable<ProgressDto>>> GetAll()
        {
            var progresses = await _progressService.GetAllAsync();
            return Ok(progresses);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<ProgressDto>> GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");
            var progress = await _progressService.GetByIdAsync<string>(id);
            if (progress == null) return NotFound();
            return Ok(progress);
        }
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ProgressDto>>> GetByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("Invalid userId");
            var progresses = await _progressService.GetByUserIdAsync(userId);
            return Ok(progresses);
        }
        [HttpPost]
        public async Task<ActionResult<ProgressDto>> Create([FromBody] ProgressDto progressDto)
        {
            if (progressDto == null || string.IsNullOrEmpty(progressDto.UserId))
                return BadRequest("Invalid progress data");
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
            return CreatedAtAction(nameof(GetById), new { id = progress.ProgressId }, progress);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ProgressDto progressDto)
        {
            if (string.IsNullOrEmpty(id) || progressDto == null) return BadRequest("Invalid input");
            var progress = await _progressService.GetByIdAsync<string>(id);
            if (progress == null) return NotFound();

            progress.UserId = progressDto.UserId;
            progress.DailyConversationCount = progressDto.DailyConversationCount;
            progress.TotalConversationTimeMinutes = progressDto.TotalConversationTimeMinutes;
            progress.CurrentStreakDays = progressDto.CurrentStreakDays;
            progress.LongestStreakDays = progressDto.LongestStreakDays;
            progress.CurrentLevel = progressDto.CurrentLevel;
            progress.LastConversationDate = progressDto.LastConversationDate;
            progress.UpdatedAt = DateTime.UtcNow;

            await _progressService.UpdateAsync(progress);
            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");
            var progress = await _progressService.GetByIdAsync<string>(id);
            if (progress == null) return NotFound();

            await _progressService.DeleteAsync(progress);
            return NoContent();
        }
    }
}



