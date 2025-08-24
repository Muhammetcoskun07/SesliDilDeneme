using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromptsController : ControllerBase
    {
        private readonly PromptService _promptService;

        public PromptsController(PromptService promptService)
        {
            _promptService = promptService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var prompts = await _promptService.GetAllAsync();
            var result = prompts.Select(p => new
            {
                p.PromptId,
                p.AgentId,
                AgentName = p.Agent?.AgentName,
                p.Title,
                p.Content,
                p.PromptMessage 
            });
            return Ok(result);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var prompt = await _promptService.GetByIdAsync(id);
            if (prompt == null)
                throw new KeyNotFoundException();

            var result = new
            {
                prompt.PromptId,
                prompt.AgentId,
                AgentName = prompt.Agent?.AgentName,
                prompt.Title,
                prompt.Content,
                prompt.PromptMessage // yeni
            };

            return Ok(result);
        }

        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetByAgent(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("Geçersiz agentId.");

            var prompts = await _promptService.GetByAgentAsync(agentId);
            if (prompts == null || !prompts.Any())
                throw new KeyNotFoundException();

            var result = prompts.Select(p => new
            {
                p.PromptId,
                p.AgentId,
                AgentName = p.Agent?.AgentName,
                p.Title,
                p.Content,
                p.PromptMessage
            });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PromptCreateDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.AgentId) ||
                string.IsNullOrWhiteSpace(dto.Title) ||
                string.IsNullOrWhiteSpace(dto.Content))
                throw new ArgumentException("AgentId, Title ve Content zorunludur.");

            var prompt = new Prompt
            {
                PromptId = Guid.NewGuid().ToString(),
                AgentId = dto.AgentId,
                Title = dto.Title,
                Content = dto.Content,
                PromptMessage = dto.PromptMessage // yeni
            };

            await _promptService.CreateAsync(prompt);

            var createdPrompt = await _promptService.GetByIdAsync(prompt.PromptId);

            var result = new
            {
                createdPrompt!.PromptId,
                createdPrompt.AgentId,
                AgentName = createdPrompt.Agent?.AgentName,
                createdPrompt.Title,
                createdPrompt.Content,
                createdPrompt.PromptMessage // yeni
            };

            return CreatedAtAction(nameof(GetById), new { id = prompt.PromptId }, result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var prompt = await _promptService.GetByIdAsync(id);
            if (prompt == null)
                throw new KeyNotFoundException();

            await _promptService.DeleteAsync(prompt);

            return Ok(new { Deleted = true, Id = id });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] PromptUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var prompt = await _promptService.GetByIdAsync(id);
            if (prompt == null)
                throw new KeyNotFoundException();

            if (!string.IsNullOrWhiteSpace(dto.Title)) prompt.Title = dto.Title;
            if (!string.IsNullOrWhiteSpace(dto.Content)) prompt.Content = dto.Content;
            if (dto.PromptMessage != null) prompt.PromptMessage = dto.PromptMessage;

            await _promptService.UpdateAsync(prompt);

            return Ok(new { Updated = true, id });
        }
    }
}
