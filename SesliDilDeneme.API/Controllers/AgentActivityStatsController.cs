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

        // GET: api/AgentActivityStats/{userId}/{agentId}
        [HttpGet("{userId}/{agentId}")]
        public async Task<IActionResult> GetAgentStatsForUser(string userId, string agentId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("UserId ve AgentId zorunludur.");

            var stats = await _statsService.GetAgentStatsForUserAsync(userId, agentId);
            if (stats is null)
                throw new KeyNotFoundException("İlgili user/agent için istatistik bulunamadı.");

            return Ok(stats); // wrapper filter ApiResponse<T>.Ok ile saracak
        }

        // GET: api/AgentActivityStats/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserAllAgentStats(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId zorunludur.");

            var statsList = await _statsService.GetUserAllAgentStatsAsync(userId);
            if (statsList is null || !statsList.Any())
                throw new KeyNotFoundException("Kullanıcıya ait istatistik bulunamadı.");

            return Ok(statsList); // liste/collection direkt dön
        }
    }
}
