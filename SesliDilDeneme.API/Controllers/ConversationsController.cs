using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;
using SesliDil.Core.Responses;
using Microsoft.EntityFrameworkCore;

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

        public ConversationsController(
            ConversationService conversationService,
            UserService userService,
            AgentActivityService agentActivityService,
            ILogger<ConversationsController> logger)
        {
            _conversationService = conversationService;
            _userService = userService;
            _agentActivityService = agentActivityService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var conversations = await _conversationService.GetAllAsync(); // muhtemelen IEnumerable<Conversation>
            return Ok(new ApiResponse<object>("İşlem başarılı.", conversations));
        }

        [HttpGet("id/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new ApiResponse<object>("Invalid id", null));

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null)
                return NotFound(new ApiResponse<object>("Not found", null));

            return Ok(new ApiResponse<object>("İşlem başarılı.", conversation));
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new ApiResponse<object>("Invalid userId", null));

            var conversations = await _conversationService.GetByUserIdAsync(userId);
            return Ok(new ApiResponse<object>("İşlem başarılı.", conversations));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConversationDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.UserId))
                return BadRequest(new ApiResponse<object>("Invalid conversation data", null));

            var user = await _userService.GetByIdAsync(dto.UserId);
            if (user == null)
                return BadRequest(new ApiResponse<object>("User not found", null));

            var conversation = new Conversation
            {
                ConversationId = Guid.NewGuid().ToString(),
                UserId = dto.UserId,
                AgentId = dto.AgentId,
                Title = dto.Title,
                Language = user.TargetLanguage,
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _conversationService.CreateAsync(conversation);

            var responseDto = new ConversationDto
            {
                ConversationId = conversation.ConversationId,
                UserId = conversation.UserId,
                AgentId = conversation.AgentId,
                Title = conversation.Title,
                DurationMinutes = conversation.DurationMinutes
            };

            return CreatedAtAction(nameof(GetById), new { id = conversation.ConversationId },
                new ApiResponse<object>("İşlem başarılı.", responseDto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ConversationDto conversationDto)
        {
            if (string.IsNullOrEmpty(id) || conversationDto == null)
                return BadRequest(new ApiResponse<object>("Invalid input", null));

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null)
                return NotFound(new ApiResponse<object>("Not found", null));
     
            var user = await _userService.GetByIdAsync(conversationDto.UserId);
            if (user == null)
                return BadRequest(new ApiResponse<object>("User not found", null));

            conversation.UserId = conversationDto.UserId;
            conversation.AgentId = conversationDto.AgentId;
            conversation.Title = conversationDto.Title;
            conversation.Language = user.TargetLanguage;
            conversation.DurationMinutes = conversationDto.DurationMinutes;

            await _conversationService.UpdateAsync(conversation);
            return Ok(new ApiResponse<object>("İşlem başarılı.", null));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new ApiResponse<object>("Invalid id", null));

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null)
                return NotFound(new ApiResponse<object>("Not found", null));

            await _conversationService.DeleteAsync(conversation);
            return Ok(new ApiResponse<object>("İşlem başarılı.", null));
        }

        [HttpGet("{id}/duration")]
        public async Task<IActionResult> GetDuration(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new ApiResponse<object>("Invalid id", null));

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null)
                return NotFound(new ApiResponse<object>("Not found", null));

            var minutes = conversation.DurationMinutes ?? (DateTime.UtcNow - conversation.StartedAt).TotalMinutes;
            return Ok(new ApiResponse<object>("İşlem başarılı.", minutes));
        }

        [HttpGet("{conversationId}/summary")]
        public async Task<IActionResult> GetConversationSummary(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return BadRequest(new
                {
                    message = "Hata: ConversationId gerekli.",
                    error = "InvalidParameter",
                    body = (string)null
                });

            var result = await _conversationService.GetConversationSummaryAsync(conversationId);

            return Ok(new
            {
                message = "İşlem başarılı.",
                error = (string)null,
                body = result
            });
        }
        [HttpGet("summary/computed/{conversationId}")]
        public async Task<IActionResult> GetComputedSummary(
           string conversationId,
           [FromQuery] int samples = 3,
           [FromQuery] int highlights = 3)
        {
            var dto = await _conversationService.BuildConversationSummaryComputedAsync(conversationId, samples, highlights);
            return Ok(new ApiResponse<object>("İşlem başarılı.", dto));
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
     
       

        [HttpGet("{conversationId}/user-grammar-errors")]
        public async Task<IActionResult> GetUserGrammarErrors(string conversationId)
        {
            var messages = await _conversationService.GetUserMessagesWithGrammarErrorsAsync(conversationId);

            var result = messages.Select(m => new
            {
                m.MessageId,
                m.Content,
                GrammarErrors = m.GrammarErrors,
                CorrectedText = m.CorrectedText
            });

            return Ok(new
            {
                message = "İşlem başarılı.",
                error = (string)null,
                body = result
            });
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchConversations([FromQuery] string query)
        {
            var conversations = await _conversationService.SearchConversationsAsync(query);

            var result = conversations.Select(c => new
            {
                c.ConversationId,
                c.Title,
                c.Summary,
                c.UserId,
                c.AgentId,
                c.CreatedAt
            });

            return Ok(new
            {
                message = "İşlem başarılı.",
                error = (string)null,
                body = result
            });
        }
        [HttpGet("user/{userId}/agent/{agentId}/grammar-errors")]
        public async Task<IActionResult> GetUserGrammarErrorsByAgent(string userId, string agentId)
        {
            var messages = await _conversationService.GetUserMessagesWithGrammarErrorsByAgentAsync(userId, agentId);

            var result = messages.Select(m => new
            {
                m.MessageId,
                m.ConversationId,
                m.Content,
                GrammarErrors = m.GrammarErrors,
                CorrectedText=m.CorrectedText
            });

            return Ok(new
            {
                message = "İşlem başarılı.",
                error = (string)null,
                body = result
            });
        }
        [HttpGet("user/{userId}/agent/{agentId}/conversations")]
        public async Task<IActionResult> GetConversationsByUserAndAgent(string userId, string agentId)
        {
            var conversations = await _conversationService.GetConversationsByUserAndAgentAsync(userId, agentId);

            var result = conversations.Select(c => new
            {
                c.ConversationId,
                c.Title,
                c.Language,
                c.StartedAt,
                c.CreatedAt,
                c.DurationMinutes,
                c.Summary
            });

            return Ok(new
            {
                message = "İşlem başarılı.",
                error = (string)null,
                body = result
            });
        }
    }
}
