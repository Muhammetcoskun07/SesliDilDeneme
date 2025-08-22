using System;
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
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var session = await _sessionService.GetByIdAsync<string>(id);
            if (session is null)
                throw new KeyNotFoundException();

            return Ok(session);
        }

        // POST: api/session
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SessionDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.UserId))
                throw new ArgumentException("UserId zorunludur.");

            var session = new Session
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = dto.UserId,
                CreatedAt = DateTime.UtcNow
            };

            await _sessionService.CreateAsync(session);

            return CreatedAtAction(nameof(GetById), new { id = session.SessionId }, session);
        }

        // DELETE: api/session/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var session = await _sessionService.GetByIdAsync<string>(id);
            if (session is null)
                throw new KeyNotFoundException();

            await _sessionService.DeleteAsync(session);
            return Ok(new { Deleted = true, Id = id });
        }
    }
}
