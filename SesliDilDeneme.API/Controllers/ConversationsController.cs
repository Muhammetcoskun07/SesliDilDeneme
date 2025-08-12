using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;
using SesliDil.Core.Responses;

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

        [HttpGet("{id}/summary")]
        public async Task<IActionResult> GetSummary(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new ApiResponse<object>("Invalid id", null));

            var summary = await _conversationService.GetSummaryByConversationIdAsync(id);
            return Ok(new ApiResponse<object>("İşlem başarılı.", summary));
        }

        //[HttpPost("{id}/end")]
        //public async Task<IActionResult> EndConversation(string id)
        //{
        //    if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");

        //    var conversation = await _conversationService.GetByIdAsync<string>(id);
        //    if (conversation == null) return NotFound();

        //    await _conversationService.EndConversationAsync(id);
        //    return NoContent();
        //}


       
        [HttpPost("{id}/summary")]
        public async Task<IActionResult> SaveSummary(string id, [FromBody] string summary)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(summary))
                return BadRequest(new ApiResponse<object>("Geçersiz giriş", null));

            try
            {
                await _conversationService.SaveSummaryAsync(id, summary);
                return Ok(new ApiResponse<object>("İşlem başarılı.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>("Hata oluştu", null, ex.Message));
            }
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
    }
}
