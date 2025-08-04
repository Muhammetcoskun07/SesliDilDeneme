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
        private readonly IRepository<User> _userRepository;
        public MessageController(MessageService messageService,IRepository<User> userRepository)
        {
            _messageService = messageService;
            _userRepository = userRepository;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetAll()
        {
            var messages = await _messageService.GetAllAsync();
            return Ok(messages);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Invalid id");
            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null) return BadRequest(string.Empty);
            return Ok(message);
        }
        [HttpGet("conversation/{conversationid}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetByConversationId(string conversationid)
        {
            if (string.IsNullOrWhiteSpace(conversationid)) return BadRequest("Invalid conversationid");
            var messages = await _messageService.GetMessagesByConversationIdAsync(conversationid);
            return Ok(messages);
        }
        [HttpPost]
        public async Task<ActionResult<MessageDto>> Create([FromBody] MessageDto messageDto)
        {
            if (messageDto == null || string.IsNullOrEmpty(messageDto.ConversationId) || string.IsNullOrEmpty(messageDto.Role)
                || string.IsNullOrEmpty(messageDto.Content)) return BadRequest("Invalid message data");
            var message = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = messageDto.ConversationId,
                Role = messageDto.Role,
                Content = messageDto.Content,
                AudioUrl = messageDto.AudioUrl,
                SpeakerType = messageDto.SpeakerType,
                CreatedAt = DateTime.UtcNow
            };
            await _messageService.CreateAsync(new Message
            {
                MessageId = message.MessageId,
                ConversationId = message.ConversationId,
                Role = message.Role,
                Content = message.Content,
                SpeakerType = message.SpeakerType,
                CreatedAt = message.CreatedAt,
                AudioUrl = message.AudioUrl ?? "",             // boş string
                TranslatedContent = message.TranslatedContent ?? "",
                GrammarErrors = message.GrammarErrors ?? new List<string>()
            });
            return CreatedAtAction(nameof(GetById), new { id = message.MessageId, message });
            //  return Ok(message);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] MessageDto messageDto)
        {
            if (string.IsNullOrEmpty(id) || messageDto == null) return BadRequest("Invalid input");
            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null) return NotFound();
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
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");
            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null) return NotFound();

            await _messageService.DeleteAsync(message);
            return NoContent();
        }
        [HttpPost("send")]
        public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.AgentId))
                return BadRequest("Invalid input");

            // Kullanıcıyı veritabanından çek
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null) return NotFound("User not found");

            // Kullanıcının hedef dilini kullan
            var targetLanguage = user.TargetLanguage;
            if (string.IsNullOrWhiteSpace(targetLanguage))
                return BadRequest("User's target language is not specified");

            var message = new MessageDto
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                Role = "user",
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                GrammarErrors = new List<string>()
            };

            var translated = await _messageService.TranslateAsync(message.Content, targetLanguage, request.AgentId);
            var grammar = await _messageService.CheckGrammarAsync(message.Content, request.AgentId);

            message.TranslatedContent = translated;
            message.GrammarErrors = grammar;

            await _messageService.CreateAsync(new Message
            {
                MessageId = message.MessageId,
                ConversationId = message.ConversationId,
                Role = message.Role,
                Content = message.Content,
                SpeakerType = message.SpeakerType,
                CreatedAt = message.CreatedAt
            });

            return Ok(message);
        }
        [HttpGet("translated")]
        public async Task<IActionResult> GetTranslatedMessage([FromQuery] string messageId)
        {
            var message = await _messageService.GetByIdAsync(messageId);
            if (message == null)
                return NotFound();

            var response = new TranslatedContentResponse
            {
                TranslatedContent = message.TranslatedContent
            };

            return Ok(response);
        }



    }
}
