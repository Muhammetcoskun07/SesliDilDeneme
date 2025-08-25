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
        private readonly ILogger<ConversationsController> _logger;

        public ConversationsController(
            ConversationService conversationService,
            ILogger<ConversationsController> logger)
        {
            _conversationService = conversationService;
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
            var conversation = await _conversationService.GetByIdOrThrowAsync(id);
            return Ok(conversation);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            var conversations = await _conversationService.GetByUserIdAsync(userId);
            return Ok(conversations);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConversationDto dto)
        {
            var responseDto = await _conversationService.CreateFromDtoAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = responseDto.ConversationId }, responseDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ConversationDto conversationDto)
        {
            var conversation = await _conversationService.UpdateFromDtoAsync(id, conversationDto);
            return Ok(conversation);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _conversationService.DeleteByIdAsync(id);
            return Ok(new { Deleted = true, Id = id });
        }

        [HttpGet("{id}/duration")]
        public async Task<IActionResult> GetDuration(string id)
        {
            var minutes = await _conversationService.GetDurationAsync(id);
            return Ok(minutes);
        }

        [HttpGet("{conversationId}/summary")]
        public async Task<IActionResult> GetConversationSummary(string conversationId)
        {
            var result = await _conversationService.GetConversationSummaryAsync(conversationId);
            return Ok(result);
        }

        [HttpGet("summary/computed/{conversationId}")]
        public async Task<IActionResult> GetComputedSummary(
            string conversationId,
            [FromQuery] int samples = 3,
            [FromQuery] int highlights = 3)
        {
            var dto = await _conversationService.BuildConversationSummaryComputedAsync(conversationId, samples, highlights);
            return Ok(dto);
        }

        [HttpPost("{id}/end")]
        public async Task<IActionResult> EndConversation(string id)
        {
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
