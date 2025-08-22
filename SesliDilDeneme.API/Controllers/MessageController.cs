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
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz mesaj kimliği.");

            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null)
                throw new KeyNotFoundException();

            return Ok(message);
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetByConversationId(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("Geçersiz konuşma kimliği.");

            var messages = await _messageService.GetMessagesByConversationIdAsync(conversationId);
            return Ok(messages);
        }

        [HttpPost("speak")]
        public async Task<IActionResult> SpeakByMessageId([FromBody] SpeakByMessageIdRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.MessageId))
                throw new ArgumentException("MessageId zorunludur.");

            var message = await _messageRepository.GetByIdAsync(request.MessageId);
            if (message == null)
                throw new KeyNotFoundException();

            var conversation = await _conversationRepository.GetByIdAsync(message.ConversationId);
            if (conversation == null)
                throw new KeyNotFoundException();

            var user = await _userRepository.GetByIdAsync(conversation.UserId);
            if (user == null)
                throw new KeyNotFoundException();

            if (string.IsNullOrWhiteSpace(user.TargetLanguage))
                throw new ArgumentException("Kullanıcının hedef dili ayarlı değil.");

            var audioBytes = await _ttsService.ConvertTextToSpeechAsync(message.Content, "alloy");
            var relativeUrl = await _ttsService.SaveAudioToFileAsync(audioBytes);
            var baseUrl = _configuration["AppUrl"]?.TrimEnd('/') ?? "";
            var fullUrl = $"{baseUrl}{relativeUrl}";

            return Ok(new { AudioUrl = fullUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MessageDto messageDto)
        {
            // NOT: FluentValidation -> ModelState’e yazacak; invalid ise ValidationFilter 400 döndürecek

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
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz mesaj kimliği.");
            // FluentValidation => ModelState’e yazar

            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null)
                throw new KeyNotFoundException();

            message.ConversationId = messageDto.ConversationId;
            message.Role = messageDto.Role;
            message.Content = messageDto.Content;
            message.AudioUrl = messageDto.AudioUrl;
            message.SpeakerType = messageDto.SpeakerType;
            message.CreatedAt = DateTime.UtcNow;
            message.TranslatedContent = messageDto.TranslatedContent ?? message.TranslatedContent;
            message.GrammarErrors = messageDto.GrammarErrors ?? message.GrammarErrors;

            await _messageService.UpdateAsync(message);
            return Ok(message);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz mesaj kimliği.");

            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null)
                throw new KeyNotFoundException();

            await _messageService.DeleteAsync(message);
            // Boş dönersen filter 200 + ApiResponse.Ok yapar
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
            if (string.IsNullOrWhiteSpace(messageId))
                throw new ArgumentException("MessageId zorunludur.");

            var message = await _messageService.GetByIdAsync(messageId);
            if (message == null)
                throw new KeyNotFoundException();

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
                throw new ArgumentException("MessageId zorunludur.");

            var message = await _messageService.GetByIdAsync(messageId);
            if (message == null)
                throw new KeyNotFoundException();

            return Ok(message.GrammarErrors);
        }
    }
}
