using Microsoft.AspNetCore.Mvc;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIAgentController : ControllerBase
    {
        private readonly AIAgentService _agentService;

        public AIAgentController(AIAgentService agentService)
        {
            _agentService = agentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllActiveAgents()
        {
            var agents = await _agentService.GetActiveAgentsAsync();
            return Ok(agents);
        }

        [HttpGet("type/{agentType}")]
        public async Task<IActionResult> GetByType(string agentType)
        {
            var agent = await _agentService.GetByTypeAsync(agentType);
            if (agent == null)
                return NotFound("Ajan bulunamadı");

            return Ok(agent);
        }
    }
}
