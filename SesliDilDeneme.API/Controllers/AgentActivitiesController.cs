using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentActivitiesController : ControllerBase
    {
        private readonly IRepository<ConversationAgentActivity> _activityRepository; // veya Service
        private readonly ILogger<AgentActivitiesController> _logger;

        public AgentActivitiesController(IRepository<ConversationAgentActivity> activityRepository, ILogger<AgentActivitiesController> logger)
        {
            _activityRepository = activityRepository;
            _logger = logger;
        }

        [HttpGet("{activityId}")]
        public async Task<IActionResult> GetByActivityId(string activityId)
        {
            try
            {
                var activity = await _activityRepository.GetByIdAsync(activityId);
                if (activity == null)
                    return NotFound(new { Message = $"Activity with id {activityId} not found." });

                return Ok(new
                {
                    ActivityId = activity.ActivityId,
                    ConversationId = activity.ConversationId,
                    UserId = activity.UserId,
                    AgentId = activity.AgentId,
                    Duration = activity.Duration,
                    MessageCount = activity.MessageCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching activity with id {ActivityId}", activityId);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetByConversationId(string conversationId)
        {
            var activities = await _activityRepository
      .Query()
      .Where(a => a.ConversationId == conversationId)
      .ToListAsync();

            if (!activities.Any())
                return NotFound();

            return Ok(activities);
        }

    }
}
