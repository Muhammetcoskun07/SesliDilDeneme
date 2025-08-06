using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Data.Repositories;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly MessageService _messageService;
        private readonly IValidator<MessageDto> _messageValidator;
        private readonly IValidator<SendMessageRequest> _sendMessageValidator;
        private readonly IMapper _mapper;

        public MessageController(
            MessageService messageService,
            IValidator<MessageDto> messageValidator,
            IValidator<SendMessageRequest> sendMessageValidator,
            IMapper mapper)
        {
            _messageService = messageService;
            _messageValidator = messageValidator;
            _sendMessageValidator = sendMessageValidator;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetAll()
        {
            var messages = await _messageService.GetAllAsync();
            return Ok(messages);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MessageDto>> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Invalid id");

            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null)
                return NotFound("Message not found");

            return Ok(message);
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetByConversationId(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return BadRequest("Invalid conversation id");

            var messages = await _messageService.GetMessagesByConversationIdAsync(conversationId);
            return Ok(messages);
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> Create([FromBody] MessageDto messageDto)
        {
            var validationResult = await _messageValidator.ValidateAsync(messageDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var message = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = messageDto.ConversationId,
                Role = messageDto.Role,
                Content = messageDto.Content,
                AudioUrl = messageDto.AudioUrl ?? "",
                SpeakerType = messageDto.SpeakerType,
                CreatedAt = DateTime.UtcNow,
                TranslatedContent = messageDto.TranslatedContent ?? "",
                GrammarErrors = messageDto.GrammarErrors ?? new List<string>()
            };

            await _messageService.CreateAsync(message);
            var messageDtoResponse = _mapper.Map<MessageDto>(message);
            return CreatedAtAction(nameof(GetById), new { id = message.MessageId }, messageDtoResponse);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] MessageDto messageDto)
        {
            if (string.IsNullOrEmpty(id) || messageDto == null)
                return BadRequest("Invalid input");

            var validationResult = await _messageValidator.ValidateAsync(messageDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null)
                return NotFound("Message not found");

            message.ConversationId = messageDto.ConversationId;
            message.Role = messageDto.Role;
            message.Content = messageDto.Content;
            message.AudioUrl = messageDto.AudioUrl;
            message.SpeakerType = messageDto.SpeakerType;
            message.CreatedAt = DateTime.UtcNow;

            await _messageService.UpdateAsync(message);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Invalid id");

            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null)
                return NotFound("Message not found");

            await _messageService.DeleteAsync(message);
            return NoContent();
        }

        [HttpPost("send")]
        public async Task<ActionResult<MessageDto>> Send([FromBody] SendMessageRequest request)
        {
            var validationResult = await _sendMessageValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            try
            {
                var response = await _messageService.SendMessageAsync(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("translated")]
        public async Task<IActionResult> GetTranslatedMessage([FromQuery] string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return BadRequest("MessageId is required");

            var message = await _messageService.GetByIdAsync(messageId);
            if (message == null)
                return NotFound("Message not found");

            var response = new TranslatedContentResponse
            {
                TranslatedContent = message.TranslatedContent
            };

            return Ok(response);
        }

        [HttpGet("{messageId}/grammar-errors")]
        public async Task<IActionResult> GetGrammarErrors(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return BadRequest("MessageId is required");

            var message = await _messageService.GetByIdAsync(messageId);
            if (message == null)
                return NotFound("Message not found");

            return Ok(message.GrammarErrors);
        }
    }
}
