using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly SessionService _sessionService;
        private readonly IConfiguration _configuration;

        public AuthController(UserService userService, IConfiguration configuration, SessionService sessionService)
        {
            _userService = userService;
            _configuration = configuration;
            _sessionService = sessionService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SocialLoginDto socialLoginDto)
        {
            if (socialLoginDto == null || string.IsNullOrEmpty(socialLoginDto.Provider) || string.IsNullOrEmpty(socialLoginDto.IdToken))
                return BadRequest("Invalid social login data");

            User user = null;

            if (socialLoginDto.Provider.ToLower() == "apple")
            {
                user = await HandleAppleLogin(socialLoginDto.IdToken);
            }
            else if (socialLoginDto.Provider.ToLower() == "google")
            {
                user = await HandleGoogleLogin(socialLoginDto.IdToken);
            }
            else
            {
                return BadRequest("Unsupported provider");
            }

            if (user == null) return Unauthorized("Invalid token or user");

            // ✅ Session kaydı oluştur
            await _sessionService.CreateAsync(new Session
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow
            });

            // ✅ JWT Token üret
            var token = GenerateJwtToken(user);

            return Ok(new { Token = token, UserId = user.UserId });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Eğer ileride token blacklist yapacaksan burada Redis gibi sisteme token ekleyebilirsin
            return Ok(new { message = "Logout successful." });
        }

        private async Task<User> HandleGoogleLogin(string idToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(idToken);

            // Basit doğrulama (prod ortamda issuer/audience kontrolü zorunlu)
            if (token.Payload.Iss != "https://accounts.google.com" || !token.Payload.Aud.Contains(_configuration["Google:ClientId"]))
                return null;

            var socialId = token.Payload.Sub;

            var user = await _userService.GetOrCreateBySocialAsync("google", socialId, null, "GoogleUser", "LastName");
            return user;
        }

        private async Task<User> HandleAppleLogin(string idToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(idToken);

                if (jwtToken.Payload.Iss != "https://appleid.apple.com" ||
                    !jwtToken.Payload.Aud.Contains(_configuration["Apple:ClientId"]))
                {
                    return null;
                }

                var socialId = jwtToken.Payload.Sub;
                if (string.IsNullOrEmpty(socialId))
                    return null;

                var user = await _userService.GetOrCreateBySocialAsync("apple", socialId, null, "AppleUser", "LastName");
                return user;
            }
            catch
            {
                return null;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, "User")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
