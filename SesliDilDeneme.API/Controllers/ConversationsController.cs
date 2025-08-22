using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;
using System.Linq;

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
            var conversations = await _conversationService.GetAllAsync();
            return Ok(conversations);
        }

        [HttpGet("id/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null)
                throw new KeyNotFoundException();

            return Ok(conversation);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Geçersiz userId.");

            var conversations = await _conversationService.GetByUserIdAsync(userId);
            return Ok(conversations);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConversationDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.UserId))
                throw new ArgumentException("Geçersiz konuşma verisi.");

            var user = await _userService.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new KeyNotFoundException();

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

            return CreatedAtAction(nameof(GetById), new { id = conversation.ConversationId }, responseDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ConversationDto conversationDto)
        {
            if (string.IsNullOrWhiteSpace(id) || conversationDto == null)
                throw new ArgumentException("Geçersiz giriş.");

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null)
                throw new KeyNotFoundException();

            var user = await _userService.GetByIdAsync(conversationDto.UserId);
            if (user == null)
                throw new KeyNotFoundException();

            conversation.UserId = conversationDto.UserId;
            conversation.AgentId = conversationDto.AgentId;
            conversation.Title = conversationDto.Title;
            conversation.Language = user.TargetLanguage;
            conversation.DurationMinutes = conversationDto.DurationMinutes;

            await _conversationService.UpdateAsync(conversation);
            return Ok(conversation);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null)
                throw new KeyNotFoundException();

            await _conversationService.DeleteAsync(conversation);
            return Ok(new { Deleted = true, Id = id });
        }

        [HttpGet("{id}/duration")]
        public async Task<IActionResult> GetDuration(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null)
                throw new KeyNotFoundException();

            var minutes = conversation.DurationMinutes ?? (DateTime.UtcNow - conversation.StartedAt).TotalMinutes;
            return Ok(minutes);
        }

        [HttpGet("{conversationId}/summary")]
        public async Task<IActionResult> GetConversationSummary(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("ConversationId gerekli.");

            var result = await _conversationService.GetConversationSummaryAsync(conversationId);
            return Ok(result);
        }

        [HttpGet("summary/computed/{conversationId}")]
        public async Task<IActionResult> GetComputedSummary(
            string conversationId,
            [FromQuery] int samples = 3,
            [FromQuery] int highlights = 3)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("ConversationId gerekli.");

            var dto = await _conversationService.BuildConversationSummaryComputedAsync(conversationId, samples, highlights);
            return Ok(dto);
        }

        [HttpPost("{id}/end")]
        public async Task<IActionResult> EndConversation(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var conversation = await _conversationService.GetByIdAsync<string>(id);
            if (conversation == null)
                throw new KeyNotFoundException();

            var summaryResult = await _conversationService.EndConversationAsync(id);

            return Ok(new
            {
                conversationId = id,
                summary = summaryResult.Summary,
                title = summaryResult.Title
            });
        }

        [HttpGet("{conversationId}/user-grammar-errors")]
        public async Task<IActionResult> GetUserGrammarErrors(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("ConversationId gerekli.");

            var messages = await _conversationService.GetUserMessagesWithGrammarErrorsAsync(conversationId);

            var result = messages.Select(m => new
            {
                m.MessageId,
                m.Content,
                m.GrammarErrors,
                m.CorrectedText
            });

            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchConversations([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("query gerekli.");

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

            return Ok(result);
        }

        [HttpGet("user/{userId}/agent/{agentId}/grammar-errors")]
        public async Task<IActionResult> GetUserGrammarErrorsByAgent(string userId, string agentId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("userId/agentId gerekli.");

            var messages = await _conversationService.GetUserMessagesWithGrammarErrorsByAgentAsync(userId, agentId);

            var result = messages.Select(m => new
            {
                m.MessageId,
                m.ConversationId,
                m.Content,
                m.GrammarErrors,
                m.CorrectedText
            });

            return Ok(result);
        }

        [HttpGet("user/{userId}/agent/{agentId}/conversations")]
        public async Task<IActionResult> GetConversationsByUserAndAgent(string userId, string agentId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("userId/agentId gerekli.");

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

            return Ok(result);
        }
    }
}
