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

        public SessionController(SessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SessionDto>>> GetAll()
        {
            var sessions = await _sessionService.GetAllAsync();
            return Ok(sessions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SessionDto>> GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid ID");

            var session = await _sessionService.GetByIdAsync<string>(id);
            if (session == null) return NotFound();

            return Ok(session);
        }

        [HttpPost]
        public async Task<ActionResult<SessionDto>> Create([FromBody] SessionDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.UserId))
                return BadRequest("Invalid session data");

            var session = new Session
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = dto.UserId,
                CreatedAt = DateTime.UtcNow
            };

            await _sessionService.CreateAsync(session);
            return CreatedAtAction(nameof(GetById), new { id = session.SessionId }, session);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid ID");

            var session = await _sessionService.GetByIdAsync<string>(id);
            if (session == null) return NotFound();

            await _sessionService.DeleteAsync(session);
            return NoContent();
        }
    }
}
