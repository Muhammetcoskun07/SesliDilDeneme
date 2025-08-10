using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Interfaces;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly ConversationService _conversationService;
        private readonly UserService _userService;
        private readonly AgentActivityService _agentActivityService;
        private readonly ILogger<ConversationsController> _logger;

        public ConversationsController(ConversationService conversationService, UserService userService, AgentActivityService agentActivityService, ILogger<ConversationsController> logger)
        {
            _conversationService = conversationService;
            _userService = userService;
            _agentActivityService = agentActivityService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetAll()
        {
            var conversations = await _conversationService.GetAllAsync();
            return Ok(conversations);
        }

        [HttpGet("id/{id}")]
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
            if (conversationDto == null || string.IsNullOrEmpty(conversationDto.UserId))
                return BadRequest("Invalid conversation data");

            var user = await _userService.GetByIdAsync(conversationDto.UserId);
            if (user == null)
                return BadRequest("User not found");

            var conversation = new Conversation
            {
                ConversationId = Guid.NewGuid().ToString(),
                UserId = conversationDto.UserId,
                AgentId = conversationDto.AgentId,
                Title = conversationDto.Title,
                //Status = conversationDto.Status,
                Language = user.TargetLanguage,
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                DurationMinutes = null
            };

            await _conversationService.CreateAsync(conversation);
            return CreatedAtAction(nameof(GetById), new { id = conversation.ConversationId }, conversation);
        }
   
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ConversationDto conversationDto)
        {
            if (string.IsNullOrEmpty(id) || conversationDto == null)
                return BadRequest("Invalid input");

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null) return NotFound();

            var user = await _userService.GetByIdAsync(conversationDto.UserId);
            if (user == null) return BadRequest("User not found");

            conversation.UserId = conversationDto.UserId;
            conversation.AgentId = conversationDto.AgentId;
            conversation.Title = conversationDto.Title;
           // conversation.Status = conversationDto.Status;
            conversation.Language = user.TargetLanguage; // Güncelleme sırasında da kullanıcıdan alınmalı
            conversation.DurationMinutes = conversationDto.DurationMinutes;

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
        public async Task<ActionResult<ConversationSummaryDto>> GetSummary(string id )
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");

            var summary = await _conversationService.GetSummaryByConversationIdAsync(id);
            return Ok(summary);
        }
        [HttpPost("{id}/end")]
        public async Task<IActionResult> EndConversation(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null) return NotFound();

            await _conversationService.EndConversationAsync(id);
            return NoContent();
        }
        [HttpPost("{id}/summary")]
        public async Task<IActionResult> SaveSummary(string id, [FromBody] string summary)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(summary))
                return BadRequest("Geçersiz giriş");

            try
            {
                await _conversationService.SaveSummaryAsync(id, summary);
                return Ok();
            }
            catch (Exception ex)
            {
                // Hata burada loglanabilir (isteğe bağlı)
                return StatusCode(500, $"Hata: {ex.Message}"); // Daha açıklayıcı hata mesajı
            }
        }
    }
}
