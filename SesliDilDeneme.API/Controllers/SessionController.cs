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
            try
            {
                var sessions = await _sessionService.GetAllAsync();
                return Ok(new
                {
                    message = "Sessions fetched successfully.",
                    error = (string?)null,
                    data = sessions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAll sessions failed");
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message,
                    data = (object?)null
                });
            }
        }

        // GET: api/session/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new
                    {
                        message = "Invalid ID.",
                        error = "ID is required.",
                        data = (object?)null
                    });
                }

                var session = await _sessionService.GetByIdAsync<string>(id);
                if (session == null)
                {
                    return NotFound(new
                    {
                        message = "Session not found.",
                        error = "NOT_FOUND",
                        data = (object?)null
                    });
                }

                return Ok(new
                {
                    message = "Session fetched successfully.",
                    error = (string?)null,
                    data = session
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetById failed for {Id}", id);
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message,
                    data = (object?)null
                });
            }
        }

        // POST: api/session
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SessionDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.UserId))
                {
                    return BadRequest(new
                    {
                        message = "Invalid session data.",
                        error = "UserId is required.",
                        data = (object?)null
                    });
                }

                var session = new Session
                {
                    SessionId = Guid.NewGuid().ToString(),
                    UserId = dto.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _sessionService.CreateAsync(session);

                // CreatedAtAction da aynı response formatını koruyalım
                return CreatedAtAction(nameof(GetById), new { id = session.SessionId }, new
                {
                    message = "Session created successfully.",
                    error = (string?)null,
                    data = session
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create session failed for UserId {UserId}", dto?.UserId);
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message,
                    data = (object?)null
                });
            }
        }

        // DELETE: api/session/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new
                    {
                        message = "Invalid ID.",
                        error = "ID is required.",
                        data = (object?)null
                    });
                }

                var session = await _sessionService.GetByIdAsync<string>(id);
                if (session == null)
                {
                    return NotFound(new
                    {
                        message = "Session not found.",
                        error = "NOT_FOUND",
                        data = (object?)null
                    });
                }

                await _sessionService.DeleteAsync(session);

                // NoContent yerine standart response korunsun diye 200 OK dönüyoruz
                return Ok(new
                {
                    message = "Session deleted successfully.",
                    error = (string?)null,
                    data = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete session failed for {Id}", id);
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message,
                    data = (object?)null
                });
            }
        }
    }
}
