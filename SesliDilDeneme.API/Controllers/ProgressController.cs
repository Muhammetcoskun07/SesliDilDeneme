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
            var progress = await _progressService.GetByIdAsync<string>(id);
            return Ok(progress);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            var progresses = await _progressService.GetByUserIdAsync(userId);
            var response = progresses.Select(p =>
            {
                var levelProgress = _progressService.GetLevelProgressPercentage(p.BestWordsPerMinute, p.CurrentLevel);

                return new
                {
                    p.UserId,
                    CurrentLevel = _progressService.GetLevelName(p.CurrentLevel),
                    CurrentLevelCode = _progressService.GetLevelCode(p.CurrentLevel),
                    NextLevelCode = _progressService.GetNextLevelCode(p.CurrentLevel),
                    p.DailyConversationCount,
                    p.BestWordsPerMinute,
                    LevelProgressPercentage = levelProgress,
                    p.TotalConversationTimeMinutes,
                    p.CurrentStreakDays,
                    p.LongestStreakDays,
                    p.LastConversationDate,
                    p.UpdatedAt
                };
            });

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProgressDto progressDto)
        {
            var progress = await _progressService.CreateProgressAsync(progressDto);
            return CreatedAtAction(nameof(GetById), new { id = progress.ProgressId }, progress);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ProgressDto progressDto)
        {
            var progress = await _progressService.UpdateProgressAsync(id, progressDto);
            return Ok(progress);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _progressService.DeleteProgressAsync(id);
            return Ok(new { Deleted = true, Id = id });
        }
    }
}
