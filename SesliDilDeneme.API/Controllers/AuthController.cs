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

                // 🔐 JWT Token üretimi
                var accessToken = GenerateJwtToken(user); // Bu zaten var
                var refreshToken = Guid.NewGuid().ToString(); // Veya başka bir yöntemle üret
                var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
                var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

                // 🆕 Session'ı Access/Refresh token'larla oluştur
                await _sessionService.CreateAsync(new Session
                {
                    SessionId = Guid.NewGuid().ToString(),
                    UserId = user.UserId,
                    CreatedAt = DateTime.UtcNow,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpiresAt = accessTokenExpiresAt,
                    RefreshTokenExpiresAt = refreshTokenExpiresAt
                });

                // ✅ Geri dön
                return Ok(new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpiresAt = accessTokenExpiresAt,
                    RefreshTokenExpiresAt = refreshTokenExpiresAt,
                    UserId = user.UserId,
                    HasCompletedOnboarding = user.HasCompletedOnboarding
                });
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
                // Frontend’den gelen verileri logla
                Console.WriteLine($"Frontend Data - Email: {request.Email}, FirstName: {request.FirstName}, LastName: {request.LastName}");

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

                // Token’ın ham halini ve claim’leri logla
                Console.WriteLine($"Raw Apple Token: {request.IdToken}");
                Console.WriteLine("Apple Token Claims:");
                foreach (var claim in principal.Claims)
                {
                    Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
                }

                // Token’dan email’i al ve frontend’den gelenle karşılaştır
                var tokenEmail = principal.Claims.FirstOrDefault(c => c.Type == "email" ||
                    c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
                var email = string.IsNullOrEmpty(request.Email) ? (string.IsNullOrEmpty(tokenEmail) ? $"{socialId}@apple.local" : tokenEmail) : request.Email;
                if (!string.IsNullOrEmpty(tokenEmail) && !string.IsNullOrEmpty(request.Email) && tokenEmail != request.Email)
                {
                    Console.WriteLine($"Email mismatch: Token Email: {tokenEmail}, Frontend Email: {request.Email}");
                    return BadRequest("Email in token does not match frontend-provided email.");
                }
                email = email.Length > 255 ? email.Substring(0, 255) : email;

                // Frontend’den gelen firstName ve lastName’i kullan, eksikse token’dan veya varsayılan değerlerden al
                var firstName = string.IsNullOrEmpty(request.FirstName)
                    ? (principal.Claims.FirstOrDefault(c => c.Type == "given_name" ||
                        c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value ?? "Guest")
                    : request.FirstName;
                firstName = firstName.Length > 100 ? firstName.Substring(0, 100) : firstName;

                var lastName = string.IsNullOrEmpty(request.LastName)
                    ? (principal.Claims.FirstOrDefault(c => c.Type == "family_name" ||
                        c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")?.Value ?? "User")
                    : request.LastName;
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

                return Ok(new
                {
                    Token = jwt,
                    UserId = user.UserId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    HasCompletedOnboarding = user.HasCompletedOnboarding
                });
            }
            catch (SecurityTokenException ex)
            {
                return BadRequest(new { message = "Apple token validation failed", error = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"DbUpdateException: {innerException}");
                return BadRequest(new { message = "Database error while saving changes", error = innerException });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
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

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            var session = await _sessionService.GetByRefreshTokenAsync(refreshToken);

            if (session == null || session.RefreshTokenExpiresAt < DateTime.UtcNow)
                return Unauthorized("Refresh token geçersiz veya süresi dolmuş.");

            var user = await _userService.GetByIdAsync(session.UserId);
            if (user == null)
                return Unauthorized("Kullanıcı bulunamadı.");

            var newAccessToken = GenerateJwtToken(user);
            var newAccessExp = DateTime.UtcNow.AddMinutes(30);

            session.AccessToken = newAccessToken;
            session.AccessTokenExpiresAt = newAccessExp;
            await _sessionService.UpdateAsync(session);

            return Ok(new
            {
                AccessToken = newAccessToken,
                AccessTokenExpiresAt = newAccessExp,
                RefreshToken = session.RefreshToken,
                RefreshTokenExpiresAt = session.RefreshTokenExpiresAt,
                UserId = user.UserId
            });
        }

    }
}
