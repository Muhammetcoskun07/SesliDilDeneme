using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Interfaces;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentActivitiesController : ControllerBase
    {
        private readonly IRepository<ConversationAgentActivity> _activityRepository;
        private readonly ILogger<AgentActivitiesController> _logger;
        private readonly IAgentActivityStatsService _statsService;

        public AgentActivitiesController(
            IRepository<ConversationAgentActivity> activityRepository,
            ILogger<AgentActivitiesController> logger,
            IAgentActivityStatsService statsService)
        {
            _activityRepository = activityRepository;
            _logger = logger;
            _statsService = statsService;
        }

        // GET: api/AgentActivities/{activityId}
        [HttpGet("{activityId}")]
        public async Task<IActionResult> GetByActivityId(string activityId)
        {
            try
            {
                var activity = await _activityRepository.GetByIdAsync(activityId);
                if (activity == null)
                {
                    return NotFound(new
                    {
                        message = $"Activity with id {activityId} not found.",
                        error = "NOT_FOUND",
                        data = (object?)null
                    });
                }

                var data = new
                {
                    activity.ActivityId,
                    activity.ConversationId,
                    activity.UserId,
                    activity.AgentId,
                    activity.Duration,
                    activity.MessageCount
                };

                return Ok(new { message = "Activity fetched successfully.", error = (string?)null, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching activity with id {ActivityId}", activityId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message, data = (object?)null });
            }
        }

        // GET: api/AgentActivities/conversation/{conversationId}
        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetByConversationId(string conversationId)
        {
            try
            {
                var activities = await _activityRepository
                    .Query()
                    .Where(a => a.ConversationId == conversationId)
                    .ToListAsync();

                if (activities == null || activities.Count == 0)
                {
                    return NotFound(new
                    {
                        message = "No activities found for this conversation.",
                        error = "NOT_FOUND",
                        data = (object?)null
                    });
                }

                return Ok(new
                {
                    message = "Activities fetched successfully.",
                    error = (string?)null,
                    data = activities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching activities for conversation {ConversationId}", conversationId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message, data = (object?)null });
            }
        }

        // GET: api/AgentActivities/stats/{userId}
        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetUserAllAgentStats(string userId)
        {
            try
            {
                var stats = await _statsService.GetUserAllAgentStatsAsync(userId);
                return Ok(new { message = "User agent stats fetched successfully.", error = (string?)null, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all agent stats for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message, data = (object?)null });
            }
        }

        // GET: api/AgentActivities/stats/{userId}/{agentId}
        [HttpGet("stats/{userId}/{agentId}")]
        public async Task<IActionResult> GetAgentStats(string userId, string agentId)
        {
            try
            {
                var stat = await _statsService.GetAgentStatsForUserAsync(userId, agentId);
                if (stat == null)
                {
                    return NotFound(new
                    {
                        message = "Stats not found for given user/agent.",
                        error = "NOT_FOUND",
                        data = (object?)null
                    });
                }

                return Ok(new { message = "Agent stats fetched successfully.", error = (string?)null, data = stat });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching agent stats for user {UserId} and agent {AgentId}", userId, agentId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message, data = (object?)null });
            }
        }
    }
}
