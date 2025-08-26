using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly SessionService _sessionService;
        private readonly ILogger<SessionController> _logger;

        public SessionController(SessionService sessionService, ILogger<SessionController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        // GET: api/session
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sessions = await _sessionService.GetAllAsync();
            return Ok(sessions);
        }

        // GET: api/session/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var session = await _sessionService.GetByIdOrThrowAsync(id);
            return Ok(session);
        }

        // POST: api/session
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SessionDto dto)
        {
            // Validasyon service içinde
            var session = await _sessionService.CreateFromDtoAsync(dto?.UserId ?? string.Empty);
            return CreatedAtAction(nameof(GetById), new { id = session.SessionId }, session);
        }

        // DELETE: api/session/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _sessionService.DeleteByIdAsync(id);
            return NoContent();
        }
    }
}
