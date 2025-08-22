using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Responses;
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

        
        [HttpGet("{userId}/{agentId}")]
        public async Task<IActionResult> GetAgentStatsForUser(string userId, string agentId)
        {
            var stats = await _statsService.GetAgentStatsForUserAsync(userId, agentId);
            return Ok(ApiResponse<UserAgentStatsDto>.Ok(stats));
        }

        
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserAllAgentStats(string userId)
        {
            var statsList = await _statsService.GetUserAllAgentStatsAsync(userId);
            return Ok(statsList);
        }
    }
}
