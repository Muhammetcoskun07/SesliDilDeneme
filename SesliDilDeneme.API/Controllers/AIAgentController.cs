using System;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.Entities;
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
        public async Task<IActionResult> GetAll()
        {
            var agents = await _agentService.GetAllAsync();
            return Ok(agents);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz agent id.");

            var agent = await _agentService.GetByIdAsync<string>(id);
            if (agent is null)
                throw new KeyNotFoundException();

            return Ok(agent);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AIAgent agent)
        {
            if (agent is null)
                throw new ArgumentException("Agent verisi zorunludur.");

            agent.AgentId = Guid.NewGuid().ToString();
            await _agentService.CreateAsync(agent);

            return CreatedAtAction(nameof(GetById), new { id = agent.AgentId }, agent);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AIAgent updated)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz agent id.");
            if (updated is null)
                throw new ArgumentException("Güncelleme verisi zorunludur.");

            var agent = await _agentService.GetByIdAsync<string>(id);
            if (agent is null)
                throw new KeyNotFoundException();

            agent.AgentName = updated.AgentName;
            agent.AgentPrompt = updated.AgentPrompt;
            agent.AgentDescription = updated.AgentDescription;
            agent.AgentType = updated.AgentType;
            agent.IsActive = updated.IsActive;

            await _agentService.UpdateAsync(agent);
            return Ok(agent);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz agent id.");

            var agent = await _agentService.GetByIdAsync<string>(id);
            if (agent is null)
                throw new KeyNotFoundException();

            await _agentService.DeleteAsync(agent);
            return Ok(new { Deleted = true, Id = id });
            // İstersen sadece 200 OK mesaj için: return Ok();  (wrapper ApiResponse.Ok yapar)
        }
    }
}
