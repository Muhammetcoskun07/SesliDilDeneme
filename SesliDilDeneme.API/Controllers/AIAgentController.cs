using Microsoft.AspNetCore.Mvc;
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
            return Ok(new { message = "Agents fetched successfully.", error = (string?)null, data = agents });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var agent = await _agentService.GetByIdAsync<string>(id);
            if (agent == null)
                return NotFound(new { message = "Agent not found.", error = "NOT_FOUND", data = (object?)null });

            return Ok(new { message = "Agent fetched successfully.", error = (string?)null, data = agent });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AIAgent agent)
        {
            agent.AgentId = Guid.NewGuid().ToString();
            await _agentService.CreateAsync(agent);

            return CreatedAtAction(nameof(GetById), new { id = agent.AgentId }, new
            {
                message = "Agent created successfully.",
                error = (string?)null,
                data = agent
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AIAgent updated)
        {
            var agent = await _agentService.GetByIdAsync<string>(id);
            if (agent == null)
                return NotFound(new { message = "Agent not found.", error = "NOT_FOUND", data = (object?)null });

            agent.AgentName = updated.AgentName;
            agent.AgentPrompt = updated.AgentPrompt;
            agent.AgentDescription = updated.AgentDescription;
            agent.AgentType = updated.AgentType;
            agent.IsActive = updated.IsActive;

            await _agentService.UpdateAsync(agent);

            return Ok(new { message = "Agent updated successfully.", error = (string?)null, data = agent });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var agent = await _agentService.GetByIdAsync<string>(id);
            if (agent == null)
                return NotFound(new { message = "Agent not found.", error = "NOT_FOUND", data = (object?)null });

            await _agentService.DeleteAsync(agent);

            return Ok(new { message = "Agent deleted successfully.", error = (string?)null, data = (object?)null });
        }
    }
}
