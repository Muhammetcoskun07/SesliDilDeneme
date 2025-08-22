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
        private readonly IAgentActivityStatsService _statsService;

        public AgentActivitiesController(
            IRepository<ConversationAgentActivity> activityRepository,
            IAgentActivityStatsService statsService)
        {
            _activityRepository = activityRepository;
            _statsService = statsService;
        }

        // GET: api/AgentActivities/{activityId}
        [HttpGet("{activityId}")]
        public async Task<IActionResult> GetByActivityId(string activityId)
        {
            if (string.IsNullOrWhiteSpace(activityId))
                throw new ArgumentException("Geçersiz activityId.");

            var activity = await _activityRepository.GetByIdAsync(activityId);
            if (activity == null)
                throw new KeyNotFoundException($"Activity {activityId} bulunamadı.");

            var dto = new
            {
                activity.ActivityId,
                activity.ConversationId,
                activity.UserId,
                activity.AgentId,
                activity.Duration,
                activity.MessageCount
            };

            return Ok(dto);
        }

        // GET: api/AgentActivities/conversation/{conversationId}
        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetByConversationId(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("Geçersiz conversationId.");

            var activities = await _activityRepository
                .Query()
                .Where(a => a.ConversationId == conversationId)
                .ToListAsync();

            if (activities == null || activities.Count == 0)
                throw new KeyNotFoundException("Bu conversation için activity bulunamadı.");

            return Ok(activities);
        }

        // GET: api/AgentActivities/stats/{userId}
        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetUserAllAgentStats(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Geçersiz userId.");

            var stats = await _statsService.GetUserAllAgentStatsAsync(userId);
            if (stats == null)
                throw new KeyNotFoundException("Kullanıcıya ait istatistik bulunamadı.");

            return Ok(stats);
        }

        // GET: api/AgentActivities/stats/{userId}/{agentId}
        [HttpGet("stats/{userId}/{agentId}")]
        public async Task<IActionResult> GetAgentStats(string userId, string agentId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("UserId ve AgentId zorunludur.");

            var stat = await _statsService.GetAgentStatsForUserAsync(userId, agentId);
            if (stat == null)
                throw new KeyNotFoundException("İlgili user/agent için stat bulunamadı.");

            return Ok(stat);
        }
    }
}
