using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AIAgentController : Controller
    {
        private readonly AIAgentService _agentService;

        public AIAgentController(AIAgentService agentService)
        {
            _agentService = agentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var agents = await _agentService.GetAllAsync();
            return Ok(agents);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var agent = await _agentService.GetByIdAsync<string>(id);
            if (agent == null) return NotFound();
            return Ok(agent);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AIAgent agent)
        {
            agent.AgentId = Guid.NewGuid().ToString();
            await _agentService.CreateAsync(agent);
            return CreatedAtAction(nameof(GetById), new { id = agent.AgentId }, agent);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AIAgent updated)
        {
            var agent = await _agentService.GetByIdAsync<string>(id);
            if (agent == null) return NotFound();

            agent.AgentName = updated.AgentName;
            agent.AgentPrompt = updated.AgentPrompt;
            agent.AgentDescription = updated.AgentDescription;
            agent.AgentType = updated.AgentType;
            agent.IsActive = updated.IsActive;

            await _agentService.UpdateAsync(agent);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var agent = await _agentService.GetByIdAsync<string>(id);
            if (agent == null) return NotFound();

            await _agentService.DeleteAsync(agent);
            return NoContent();
        }
    }
}
