using Microsoft.AspNetCore.Mvc;
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
            return Ok(new
            {
                message = "Prompts retrieved successfully.",
                error = (string?)null,
                data = prompts
            });
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var prompt = await _promptService.GetByIdAsync(id);
            if (prompt == null) return NotFound();
            return Ok(new
            {
                message = "Prompt retrieved successfully.",
                error = (string?)null,
                data = prompt
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
            return Ok(new
            {
                message = "Prompts retrieved successfully.",
                error = (string?)null,
                data = prompts
            });
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Prompt prompt)
        {
            prompt.PromptId = Guid.NewGuid().ToString();
            await _promptService.CreateAsync(prompt);

            return CreatedAtAction(nameof(GetById), new { id = prompt.PromptId }, new
            {
                message = "Prompt created successfully.",
                error = (string?)null,
                data = prompt
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
                    data = (Prompt?)null
                });

            await _promptService.DeleteAsync(prompt);

            return Ok(new
            {
                message = "Prompt deleted successfully.",
                error = (string?)null,
                data = prompt
            });
        }
    }
}
