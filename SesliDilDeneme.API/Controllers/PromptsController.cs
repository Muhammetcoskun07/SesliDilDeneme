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
                p.Content
            });

            return Ok(new
            {
                message = "Prompts retrieved successfully.",
                error = (string?)null,
                data = result
            });
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var prompt = await _promptService.GetByIdAsync(id);
            if (prompt == null) return NotFound(new
            {
                message = "Prompt not found.",
                error = "NotFound",
                data = (object?)null
            });

            return Ok(new
            {
                message = "Prompt retrieved successfully.",
                error = (string?)null,
                data = new
                {
                    prompt.PromptId,
                    prompt.AgentId,
                    AgentName = prompt.Agent?.AgentName,
                    prompt.Title,
                    prompt.Content
                }
            });
        }
        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetByAgent(string agentId)
        {
            var prompts = await _promptService.GetByAgentAsync(agentId);
            if (!prompts.Any()) return NotFound(new
            {
                message = "No prompts found for the agent.",
                error = "NotFound",
                data = (object?)null
            });

            var result = prompts.Select(p => new
            {
                p.PromptId,
                p.AgentId,
                AgentName = p.Agent?.AgentName,
                p.Title,
                p.Content
            });

            return Ok(new
            {
                message = "Prompts retrieved successfully.",
                error = (string?)null,
                data = result
            });
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PromptCreateDto dto)
        {
            var prompt = new Prompt
            {
                PromptId = Guid.NewGuid().ToString(),
                AgentId = dto.AgentId,
                Title = dto.Title,
                Content = dto.Content
            };

            await _promptService.CreateAsync(prompt);

            // AgentName ile geri dönelim
            var createdPrompt = await _promptService.GetByIdAsync(prompt.PromptId);

            return CreatedAtAction(nameof(GetById), new { id = createdPrompt.PromptId }, new
            {
                message = "Prompt created successfully.",
                error = (string?)null,
                data = new
                {
                    createdPrompt.PromptId,
                    createdPrompt.AgentId,
                    AgentName = createdPrompt.Agent?.AgentName,
                    createdPrompt.Title,
                    createdPrompt.Content
                }
            });
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var prompt = await _promptService.GetByIdAsync(id);
            if (prompt == null)
                return NotFound(new
                {
                    message = "Prompt not found.",
                    error = "NotFound",
                    data = (object?)null
                });

            await _promptService.DeleteAsync(prompt);

            return Ok(new
            {
                message = "Prompt deleted successfully.",
                error = (string?)null,
                data = new
                {
                    prompt.PromptId,
                    prompt.AgentId,
                    AgentName = prompt.Agent?.AgentName,
                    prompt.Title,
                    prompt.Content
                }
            });
        }
        }
}
