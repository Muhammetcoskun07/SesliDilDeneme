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

      

       [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetByConversationId(string conversationId)
        {
             var activities = await _statsService.GetByConversationIdAsync(conversationId);
             return Ok(activities);
         }

        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetUserAllAgentStats(string userId)
        {
            var stats = await _statsService.GetUserAllAgentStatsAsync(userId);
            return Ok(stats);
        }

        
        [HttpGet("stats/{userId}/{agentId}")]
        public async Task<IActionResult> GetAgentStats(string userId, string agentId)
        {
            var stat = await _statsService.GetAgentStatsForUserAsync(userId, agentId);
            return Ok(stat);
        }
    }
}
