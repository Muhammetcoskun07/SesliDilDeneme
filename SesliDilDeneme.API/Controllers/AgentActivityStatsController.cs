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
        public async Task<IActionResult> GetAgentStatsForUser(string userId, string agentId)
        {
            var stats = await _statsService.GetAgentStatsForUserAsync(userId, agentId);

            if (stats == null)
            {
                return NotFound(new
                {
                    message = "Stats not found for given user and agent.",
                    error = "NOT_FOUND",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                message = "Agent stats fetched successfully.",
                error = (string?)null,
                data = stats
            });
        }

        // GET api/AgentActivityStats/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserAllAgentStats(string userId)
        {
            var statsList = await _statsService.GetUserAllAgentStatsAsync(userId);

            return Ok(new
            {
                message = "User agent stats fetched successfully.",
                error = (string?)null,
                data = statsList
            });
        }
    }
}
