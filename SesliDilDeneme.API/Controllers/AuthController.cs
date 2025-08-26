using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;
using SesliDil.Core.DTOs;        // GoogleLoginRequest, AppleLoginRequest burada varsayıldı
// using SesliDil.Core.Responses; // Controller içinde kullanılmıyor; wrapping filter saracak

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly SessionService _sessionService;
        private readonly IConfiguration _configuration;
        private readonly AuthService _authService;

        public AuthController(UserService userService, IConfiguration configuration, SessionService sessionService, AuthService authService)
        {
            _userService = userService;
            _configuration = configuration;
            _sessionService = sessionService;
            _authService = authService;
        }

        // ===== Strongly-typed DTO'lar =====
        public sealed class RefreshTokenRequest { public string RefreshToken { get; set; } = default!; }

        public sealed class AuthResultDto
        {
            public string AccessToken { get; init; } = default!;
            public string RefreshToken { get; init; } = default!;
            public DateTime AccessTokenExpiresAt { get; init; }
            public DateTime RefreshTokenExpiresAt { get; init; }
            public string UserId { get; init; } = default!;
            public bool HasCompletedOnboarding { get; init; }
            public string? Email { get; init; }
            public string? FirstName { get; init; }
            public string? LastName { get; init; }
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var dto = await _authService.GoogleLoginAsync(request);
            return Ok(dto); // wrapper filter ApiResponse<AuthResultDto>.Ok ile sarar
        }

        [HttpPost("apple-login")]
        public async Task<IActionResult> AppleLogin([FromBody] AppleLoginRequest request)
        {
            var dto = await _authService.AppleLoginAsync(request);
            return Ok(dto);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var dto = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(dto);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // server-side invalidation eklenecekse burada yapılır.
            return Ok(true); // bool dön; wrapper ApiResponse<bool>.Ok yapar
        }
    }
}
