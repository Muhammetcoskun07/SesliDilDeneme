using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Service.Interfaces;

namespace SesliDilDeneme.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentActivityStatsController : ControllerBase
    {

        private readonly IAgentActivityStatsService _statsService;
        public AgentActivityStatsController(IAgentActivityStatsService statsService)
        {
            _statsService = statsService;
        }
        // GET api/AgentActivityStats/{userId}/{agentId}
        [HttpGet("{userId}/{agentId}")]
        public async Task<ActionResult<UserAgentStatsDto>> GetAgentStatsForUser(string userId, string agentId)
        {
            var stats = await _statsService.GetAgentStatsForUserAsync(userId, agentId);

            if (stats == null)
                return NotFound();

            return Ok(stats);
        }
        // GET api/AgentActivityStats/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<List<UserAgentStatsDto>>> GetUserAllAgentStats(string userId)
        {
            var statsList = await _statsService.GetUserAllAgentStatsAsync(userId);

            return Ok(statsList);
        }
    }
}
