using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
        private readonly IConfiguration _configuration;
        public AuthController(UserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
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
            var token = GenerateJwtToken(user);
            return Ok(new { Token = token, UserId = user.UserId });
        }
        private async Task<User> HandleGoogleLogin(string idToken)
        {
            // Google idToken doğrulama (simplified)
            // Gerçekte: Google Token Info API ile doğrulama
            // Örnek: https://www.googleapis.com/oauth2/v3/tokeninfo?id_token={idToken}
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(idToken);

            // Dummy doğrulama (gerçekte issuer ve audience kontrolü yap)
            if (token.Payload.Iss != "https://accounts.google.com" || !token.Payload.Aud.Contains(_configuration["Google:ClientId"]))
                return null;

            var socialId = token.Payload.Sub; // Google'dan gelen unique ID
            var user = await _userService.GetOrCreateBySocialAsync("google", socialId, null, "GoogleUser", "LastName");
            return user;
        }
        private async Task<User> HandleAppleLogin(string idToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(idToken);

                // Burada dummy bir doğrulama yapıyoruz (issuer ve audience kontrolü)
                if (jwtToken.Payload.Iss != "https://appleid.apple.com" ||
                    !jwtToken.Payload.Aud.Contains(_configuration["Apple:ClientId"]))
                {
                    return null;
                }

                // sub claim'inden SocialId çıkar
                var socialId = jwtToken.Payload.Sub;
                if (string.IsNullOrEmpty(socialId))
                {
                    return null;
                }

                // Kullanıcıyı oluştur veya güncelle
                var user = await _userService.GetOrCreateBySocialAsync(
                    "apple",
                    socialId,
                    null, // Email opsiyonel, idToken'dan alınabilir
                    "AppleUser",
                    "LastName"
                );
                return user;
            }
            catch (Exception)
            {
                return null; // Token geçersizse null döndür
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
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
    }

