using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly MessageService _messageService;
        // FluentValidation otomatik devrede: manuel ValidateAsync çağırmayacağız
        private readonly IMapper _mapper;
        private readonly TtsService _ttsService;
        private readonly IRepository<User> _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IRepository<Message> _messageRepository;
        private readonly IRepository<Conversation> _conversationRepository;

        public MessageController(
            MessageService messageService,
            TtsService ttsService,
            IMapper mapper,
            IRepository<Message> messageRepository,
            IRepository<Conversation> conversationRepository,
            IRepository<User> userRepository,
            IConfiguration configuration)
        {
            _ttsService = ttsService;
            _messageService = messageService;
            _mapper = mapper;
            _userRepository = userRepository;
            _configuration = configuration;
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var messages = await _messageService.GetAllAsync();
            return Ok(messages);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var message = await _messageService.GetByIdAsync<string>(id);
            return Ok(message);
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetByConversationId(string conversationId)
        {
            var messages = await _messageService.GetMessagesByConversationIdAsync(conversationId);
            return Ok(messages);
        }
        [HttpPost("speak")]
        public async Task<IActionResult> SpeakByMessageId([FromBody] SpeakByMessageIdRequest request)
        {
            var audioUrl = await _messageService.SpeakMessageAsync(request.MessageId);
            return Ok(new { AudioUrl = audioUrl });
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MessageDto messageDto)
        {
            var createdMessage = await _messageService.CreateMessageAsync(messageDto);
            return CreatedAtAction(nameof(GetById), new { id = createdMessage.MessageId }, createdMessage);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _messageService.DeleteMessageAsync(id);
            return Ok(new { Deleted = true, Id = id });
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
        {
            // FluentValidation -> ModelState’e yazar, invalid ise ValidationFilter 400 döndürür.
            var response = await _messageService.SendMessageAsync(request);
            return Ok(response);
        }

        [HttpGet("translated")]
        public async Task<IActionResult> GetTranslatedMessage([FromQuery] string messageId)
        {
            var response = await _messageService.GetTranslatedMessageAsync(messageId);
            return Ok(response);
        }

        [HttpGet("{messageId}/grammar-errors")]
        public async Task<IActionResult> GetGrammarErrors(string messageId)
        {
            var message = await _messageService.GetByIdAsync(messageId);
            return Ok(message.GrammarErrors);
        }
    }
}
