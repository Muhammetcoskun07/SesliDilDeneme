using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly ConversationService _conversationService;
        public ConversationsController(ConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetAll()
        {
            var conversations = await _conversationService.GetAllAsync();
            return Ok(conversations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ConversationDto>> GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");
            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null) return NotFound();
            return Ok(conversation);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("Invalid userId");
            var conversations = await _conversationService.GetByUserIdAsync(userId);
            return Ok(conversations);
        }

        [HttpPost]
        public async Task<ActionResult<ConversationDto>> Create([FromBody] ConversationDto conversationDto)
        {
            if (conversationDto == null) return BadRequest("Invalid conversation data");

            var conversation = new Conversation
            {
                ConversationId = Guid.NewGuid().ToString(),
                UserId = conversationDto.UserId,
                AgentId = conversationDto.AgentId,
                Title = conversationDto.Title,
                Message = conversationDto.Message,
                Status = conversationDto.Status,
                Language = conversationDto.Language,
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                DurationMinutes = null // Başlangıçta süre null
            };

            await _conversationService.CreateAsync(conversation);
            return CreatedAtAction(nameof(GetById), new { id = conversation.ConversationId }, conversation);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ConversationDto conversationDto)
        {
            if (string.IsNullOrEmpty(id) || conversationDto == null) return BadRequest("Invalid input");

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null) return NotFound();

            conversation.UserId = conversationDto.UserId;
            conversation.AgentId = conversationDto.AgentId;
            conversation.Title = conversationDto.Title;
            conversation.Message = conversationDto.Message;
            conversation.Status = conversationDto.Status;
            conversation.Language = conversationDto.Language;
            conversation.DurationMinutes = conversationDto.DurationMinutes; // Süre istemciden gelebilir

            await _conversationService.UpdateAsync(conversation);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null) return NotFound();

            await _conversationService.DeleteAsync(conversation);
            return NoContent();
        }
        [HttpGet("{id}/duration")]
        public async Task<ActionResult<double>> GetDuration(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");
            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null) return NotFound();
            return Ok(conversation.DurationMinutes ?? (DateTime.UtcNow - conversation.StartedAt).TotalMinutes);
        }

        //  Summary Get
        [HttpGet("{id}/summary")]
        public async Task<ActionResult<ConversationSummaryDto>> GetSummary(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");

            var summary = await _conversationService.GetSummaryByConversationIdAsync(id);
            return Ok(summary);
        }

        //  Summary Save
        [HttpPost("{id}/summary")]
        public async Task<IActionResult> SaveSummary(string id, [FromBody] string summary)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(summary))
                return BadRequest("Invalid input");

            await _conversationService.SaveSummaryAsync(id, summary);
            return NoContent();
        }
    }
}
