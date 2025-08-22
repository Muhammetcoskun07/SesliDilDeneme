using System;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Responses;
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
            var agent = await _agentService.GetByIdAsync<string>(id);
            return Ok(agent);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AIAgent agent)
        {
            var createdAgentDto = await _agentService.CreateAgentAsync(agent);
            return CreatedAtAction(nameof(GetById), new { id = createdAgentDto.AgentId }, ApiResponse<AIAgentDto>.Ok(createdAgentDto));
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AIAgent updated)
        {
            var agent = await _agentService.UpdateAgentAsync(id, updated);
            return Ok(agent);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deletedId = await _agentService.DeleteAgentAsync(id);
            return Ok(deletedId);
           
        }
    }
}
