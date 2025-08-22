using System;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProgressController : ControllerBase
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
            return Ok(progresses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var progress = await _progressService.GetByIdAsync<string>(id);
            if (progress is null)
                throw new KeyNotFoundException();

            return Ok(progress);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Geçersiz userId.");

            var progresses = await _progressService.GetByUserIdAsync(userId);
            return Ok(progresses);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProgressDto progressDto)
        {
            if (progressDto is null || string.IsNullOrWhiteSpace(progressDto.UserId))
                throw new ArgumentException("Geçersiz ilerleme verisi.");

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
            if (string.IsNullOrWhiteSpace(id) || progressDto is null)
                throw new ArgumentException("Geçersiz giriş.");

            var progress = await _progressService.GetByIdAsync<string>(id);
            if (progress is null)
                throw new KeyNotFoundException();

            progress.UserId = progressDto.UserId;
            progress.DailyConversationCount = progressDto.DailyConversationCount;
            progress.TotalConversationTimeMinutes = progressDto.TotalConversationTimeMinutes;
            progress.CurrentStreakDays = progressDto.CurrentStreakDays;
            progress.LongestStreakDays = progressDto.LongestStreakDays;
            progress.CurrentLevel = progressDto.CurrentLevel;
            // İstersen gönderilen değeri esas al:
            progress.LastConversationDate = progressDto.LastConversationDate == default
                ? progress.LastConversationDate
                : progressDto.LastConversationDate;
            progress.UpdatedAt = DateTime.UtcNow;

            await _progressService.UpdateAsync(progress);
            return Ok(progress);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var progress = await _progressService.GetByIdAsync<string>(id);
            if (progress is null)
                throw new KeyNotFoundException();

            await _progressService.DeleteAsync(progress);
            return Ok(new { Deleted = true, Id = id });
        }
    }
}
