using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;
using SesliDil.Core.DTOs;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Google:ClientId"] }
                });

                if (string.IsNullOrEmpty(payload.Subject))
                    return BadRequest("Invalid SocialId from Google token.");

                var socialId = payload.Subject.Length > 255 ? payload.Subject.Substring(0, 255) : payload.Subject;
                var email = string.IsNullOrEmpty(payload.Email) ? $"{socialId}@google.local" : payload.Email.Length > 255 ? payload.Email.Substring(0, 255) : payload.Email;
                var firstName = string.IsNullOrEmpty(payload.GivenName) ? "GoogleUser" : payload.GivenName.Length > 100 ? payload.GivenName.Substring(0, 100) : payload.GivenName;
                var lastName = string.IsNullOrEmpty(payload.FamilyName) ? "GoogleLastName" : payload.FamilyName.Length > 100 ? payload.FamilyName.Substring(0, 100) : payload.FamilyName;

                var user = await _userService.GetOrCreateBySocialAsync("google", socialId, email, firstName, lastName);
                if (user == null) return Unauthorized("User creation failed");

                await _sessionService.CreateAsync(new Session
                {
                    UserId = user.UserId,
                    CreatedAt = DateTime.UtcNow
                });

                var jwtToken = GenerateJwtToken(user);
                return Ok(new { Token = jwtToken, UserId = user.UserId });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"DbUpdateException: {innerException}");
                return BadRequest(new { message = "Database error while saving changes", error = innerException });
            }
            catch (InvalidJwtException ex)
            {
                return BadRequest(new { message = "Google token validation failed", error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logout successful." });
        }


        [HttpPost("apple-login")]
        public async Task<IActionResult> AppleLogin([FromBody] AppleLoginRequest request)
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
                var principal = handler.ValidateToken(request.IdToken, validationParameters, out var validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var socialId = jwtToken.Subject;

                if (string.IsNullOrEmpty(socialId))
                    return Unauthorized("Apple ID token is invalid");

                // Apple id_token bazı durumlarda e-mail sağlamaz (ilk login harici)
                var email = principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                email = string.IsNullOrEmpty(email) ? $"{socialId}@apple.local" : email.Length > 255 ? email.Substring(0, 255) : email;
                var firstName = principal.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "AppleUser";
                firstName = firstName.Length > 100 ? firstName.Substring(0, 100) : firstName;
                var lastName = principal.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "AppleLastName";
                lastName = lastName.Length > 100 ? lastName.Substring(0, 100) : lastName;

                var user = await _userService.GetOrCreateBySocialAsync(
                    "apple", socialId, email, firstName, lastName
                );
                if (user == null)
                    return Unauthorized("User creation failed");

                // Session oluştur
                await _sessionService.CreateAsync(new Session
                {
                    SessionId = Guid.NewGuid().ToString(),
                    UserId = user.UserId,
                    CreatedAt = DateTime.UtcNow
                });

                // JWT üret
                var jwt = GenerateJwtToken(user);

                return Ok(new { Token = jwt, UserId = user.UserId });
            }
            catch (SecurityTokenException ex)
            {
                return BadRequest(new { message = "Apple token validation failed", error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine("User creation failed:");
                Console.WriteLine(ex.ToString());
                if (ex.InnerException != null)
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                throw;
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
