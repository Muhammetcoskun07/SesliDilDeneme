using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Core.Responses;
using SesliDil.Data.Repositories;
using SesliDil.Service.Services;
using System.Linq;

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
        private readonly TtsService _ttsService;
        private readonly IRepository<User> _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IRepository<Message> _messageRepository;
        private readonly IRepository<Conversation> _conversationRepository;

        public MessageController(
            MessageService messageService,
            TtsService ttsService,
            IValidator<MessageDto> messageValidator,
            IValidator<SendMessageRequest> sendMessageValidator,
            IMapper mapper,
            IRepository<Message> messageRepository,
            IRepository<Conversation> conversationRepository,
            IRepository<User> userRepository,
            IConfiguration configuration)
        {
            _ttsService = ttsService;
            _messageService = messageService;
            _messageValidator = messageValidator;
            _sendMessageValidator = sendMessageValidator;
            _mapper = mapper;
            _userRepository = userRepository;
            _configuration = configuration;
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
        }

        // -------- helper: validation hatalarını string'e çevir --------
        private static string JoinValidationErrors(ValidationResult vr)
            => string.Join("; ", vr.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var messages = await _messageService.GetAllAsync();
            return Ok(new ApiResponse<object>("İşlem başarılı.", messages, null));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new ApiResponse<object>("Fail", null, "Invalid id"));

            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null)
                return NotFound(new ApiResponse<object>("Fail", null, "Message not found"));

            return Ok(new ApiResponse<object>("İşlem başarılı.", message, null));
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetByConversationId(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return BadRequest(new ApiResponse<object>("Fail", null, "Invalid conversation id"));

            var messages = await _messageService.GetMessagesByConversationIdAsync(conversationId);
            return Ok(new ApiResponse<object>("İşlem başarılı.", messages, null));
        }

        [HttpPost("speak")]
        public async Task<IActionResult> SpeakByMessageId([FromBody] SpeakByMessageIdRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.MessageId))
                return BadRequest(new ApiResponse<object>("Fail", null, "MessageId is required."));

            var message = await _messageRepository.GetByIdAsync(request.MessageId);
            if (message == null)
                return NotFound(new ApiResponse<object>("Fail", null, "Message not found."));

            var conversation = await _conversationRepository.GetByIdAsync(message.ConversationId);
            if (conversation == null)
                return NotFound(new ApiResponse<object>("Fail", null, "Conversation not found."));

            var user = await _userRepository.GetByIdAsync(conversation.UserId);
            if (user == null)
                return NotFound(new ApiResponse<object>("Fail", null, "User not found."));

            if (string.IsNullOrWhiteSpace(user.TargetLanguage))
                return BadRequest(new ApiResponse<object>("Fail", null, "User's target language is not set."));

            var audioBytes = await _ttsService.ConvertTextToSpeechAsync(message.Content, "alloy");
            var relativeUrl = await _ttsService.SaveAudioToFileAsync(audioBytes);

            var baseUrl = _configuration["AppUrl"]?.TrimEnd('/') ?? "";
            var fullUrl = $"{baseUrl}{relativeUrl}";

            return Ok(new ApiResponse<object>("İşlem başarılı.", new { AudioUrl = fullUrl }, null));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MessageDto messageDto)
        {
            var validationResult = await _messageValidator.ValidateAsync(messageDto);
            if (!validationResult.IsValid)
                return BadRequest(new ApiResponse<object>("Fail", null, JoinValidationErrors(validationResult)));

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

            return CreatedAtAction(
                nameof(GetById),
                new { id = message.MessageId },
                new ApiResponse<object>("İşlem başarılı.", messageDtoResponse, null)
            );
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] MessageDto messageDto)
        {
            if (string.IsNullOrEmpty(id) || messageDto == null)
                return BadRequest(new ApiResponse<object>("Fail", null, "Invalid input"));

            var validationResult = await _messageValidator.ValidateAsync(messageDto);
            if (!validationResult.IsValid)
                return BadRequest(new ApiResponse<object>("Fail", null, JoinValidationErrors(validationResult)));

            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null)
                return NotFound(new ApiResponse<object>("Fail", null, "Message not found"));

            message.ConversationId = messageDto.ConversationId;
            message.Role = messageDto.Role;
            message.Content = messageDto.Content;
            message.AudioUrl = messageDto.AudioUrl;
            message.SpeakerType = messageDto.SpeakerType;
            message.CreatedAt = DateTime.UtcNow;

            await _messageService.UpdateAsync(message);
            return Ok(new ApiResponse<object>("İşlem başarılı.", null, null));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new ApiResponse<object>("Fail", null, "Invalid id"));

            var message = await _messageService.GetByIdAsync<string>(id);
            if (message == null)
                return NotFound(new ApiResponse<object>("Fail", null, "Message not found"));

            await _messageService.DeleteAsync(message);
            return Ok(new ApiResponse<object>("İşlem başarılı.", null, null));
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
        {
            var validationResult = await _sendMessageValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(new ApiResponse<object>("Fail", null, JoinValidationErrors(validationResult)));

            try
            {
                var response = await _messageService.SendMessageAsync(request);
                return Ok(new ApiResponse<object>("İşlem başarılı.", response, null));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<object>("Fail", null, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>("Fail", null, ex.Message));
            }
        }

        [HttpGet("translated")]
        public async Task<IActionResult> GetTranslatedMessage([FromQuery] string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return BadRequest(new ApiResponse<object>("Fail", null, "MessageId is required"));

            var message = await _messageService.GetByIdAsync(messageId);
            if (message == null)
                return NotFound(new ApiResponse<object>("Fail", null, "Message not found"));

            var response = new TranslatedContentResponse
            {
                TranslatedContent = message.TranslatedContent
            };

            return Ok(new ApiResponse<object>("İşlem başarılı.", response, null));
        }

        [HttpGet("{messageId}/grammar-errors")]
        public async Task<IActionResult> GetGrammarErrors(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return BadRequest(new ApiResponse<object>("Fail", null, "MessageId is required"));

            var message = await _messageService.GetByIdAsync(messageId);
            if (message == null)
                return NotFound(new ApiResponse<object>("Fail", null, "Message not found"));

            return Ok(new ApiResponse<object>("İşlem başarılı.", message.GrammarErrors, null));
        }
    }
}
