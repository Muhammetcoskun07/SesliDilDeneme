using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Interfaces;
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
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Google:ClientId"] }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            var socialId = payload.Subject;
            var email = payload.Email;
            var firstName = payload.GivenName;
            var lastName = payload.FamilyName;

            var user = await _userService.GetOrCreateBySocialAsync(
                "google", socialId, email, firstName ?? "GoogleUser", lastName ?? "LastName"
            );
            return user;
        }
        catch (Exception)
        {
            return null;
        }
    }

        private async Task<User> HandleAppleLogin(string idToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();

                // 1. Apple public keys endpoint'inden public key'leri al
                var appleKeysUrl = "https://appleid.apple.com/auth/keys";
                using var httpClient = new HttpClient();
                var json = await httpClient.GetStringAsync(appleKeysUrl);
                var keys = new JsonWebKeySet(json).Keys;

                // 2. Token’ı doğrulamak için ayarları hazırla
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://appleid.apple.com",

                    ValidateAudience = true,
                    ValidAudience = _configuration["Apple:ClientId"],

                    ValidateLifetime = true,
                    RequireExpirationTime = true,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = keys
                };

                // 3. Token'ı doğrula
                var principal = handler.ValidateToken(idToken, validationParameters, out var validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var socialId = jwtToken.Subject;

                if (string.IsNullOrEmpty(socialId))
                    return null;

                // Apple id_token bazı durumlarda e-mail sağlamaz (ilk login harici)
                var email = principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

                var user = await _userService.GetOrCreateBySocialAsync(
                    "apple", socialId, email, "AppleUser", "LastName"
                );

                return user;
            }
            catch (SecurityTokenException ex)
            {
                Console.WriteLine("Token doğrulama hatası: " + ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Genel hata: " + ex.Message);
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
