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

        public AuthController(UserService userService, IConfiguration configuration, SessionService sessionService)
        {
            _userService = userService;
            _configuration = configuration;
            _sessionService = sessionService;
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
            if (string.IsNullOrWhiteSpace(request?.IdToken))
                throw new ArgumentException("IdToken zorunludur.");

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _configuration["Google:ClientId"] }
                    });
            }
            catch (InvalidJwtException ex)
            {
                throw new ArgumentException($"Google token doğrulaması başarısız: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(payload.Subject))
                throw new ArgumentException("Geçersiz Google kimliği (subject).");

            var socialId = payload.Subject.Length > 255 ? payload.Subject[..255] : payload.Subject;
            var email = string.IsNullOrEmpty(payload.Email) ? $"{socialId}@google.local"
                          : (payload.Email.Length > 255 ? payload.Email[..255] : payload.Email);
            var firstName = string.IsNullOrEmpty(payload.GivenName) ? "GoogleUser"
                          : (payload.GivenName.Length > 100 ? payload.GivenName[..100] : payload.GivenName);
            var lastName = string.IsNullOrEmpty(payload.FamilyName) ? "GoogleLastName"
                          : (payload.FamilyName.Length > 100 ? payload.FamilyName[..100] : payload.FamilyName);

            var user = await _userService.GetOrCreateBySocialAsync("google", socialId, email, firstName, lastName);
            if (user == null)
                throw new Exception("Kullanıcı oluşturulamadı.");

            var (accessToken, refreshToken, accessExp, refreshExp) = await IssueSessionAsync(user);

            var dto = new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessExp,
                RefreshTokenExpiresAt = refreshExp,
                UserId = user.UserId,
                HasCompletedOnboarding = user.HasCompletedOnboarding
            };

            return Ok(dto); // wrapper filter ApiResponse<AuthResultDto>.Ok ile sarar
        }

        [HttpPost("apple-login")]
        public async Task<IActionResult> AppleLogin([FromBody] AppleLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.IdToken))
                throw new ArgumentException("IdToken zorunludur.");

            var handler = new JwtSecurityTokenHandler();

            JsonWebKeySet keys;
            try
            {
                using var httpClient = new HttpClient();
                var json = await httpClient.GetStringAsync("https://appleid.apple.com/auth/keys");
                keys = new JsonWebKeySet(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Apple public keys alınamadı: {ex.Message}");
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudience = _configuration["Apple:ClientId"],
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys.Keys
            };

            ClaimsPrincipal principal;
            SecurityToken validatedToken;
            try
            {
                principal = handler.ValidateToken(request.IdToken, validationParameters, out validatedToken);
            }
            catch (SecurityTokenException ex)
            {
                throw new ArgumentException($"Apple token doğrulaması başarısız: {ex.Message}");
            }

            var jwtToken = (JwtSecurityToken)validatedToken;
            var socialId = jwtToken.Subject;
            if (string.IsNullOrWhiteSpace(socialId))
                throw new ArgumentException("Geçersiz Apple kimliği (subject).");

            var tokenEmail = principal.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email)?.Value;

            var email = string.IsNullOrEmpty(request.Email)
                ? (string.IsNullOrEmpty(tokenEmail) ? $"{socialId}@apple.local" : tokenEmail)
                : request.Email;

            if (!string.IsNullOrEmpty(tokenEmail) && !string.IsNullOrEmpty(request.Email) && tokenEmail != request.Email)
                throw new ArgumentException("Email doğrulaması başarısız (EmailMismatch).");

            email = email.Length > 255 ? email[..255] : email;

            var firstName = string.IsNullOrEmpty(request.FirstName)
                ? (principal.Claims.FirstOrDefault(c => c.Type == "given_name" || c.Type == ClaimTypes.GivenName)?.Value ?? "Guest")
                : request.FirstName;
            firstName = firstName.Length > 100 ? firstName[..100] : firstName;

            var lastName = string.IsNullOrEmpty(request.LastName)
                ? (principal.Claims.FirstOrDefault(c => c.Type == "family_name" || c.Type == ClaimTypes.Surname)?.Value ?? "User")
                : request.LastName;
            lastName = lastName.Length > 100 ? lastName[..100] : lastName;

            var user = await _userService.GetOrCreateBySocialAsync("apple", socialId, email, firstName, lastName);
            if (user == null)
                throw new Exception("Kullanıcı oluşturulamadı.");

            var (accessToken, refreshToken, accessExp, refreshExp) = await IssueSessionAsync(user);

            var dto = new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessExp,
                RefreshTokenExpiresAt = refreshExp,
                UserId = user.UserId,
                HasCompletedOnboarding = user.HasCompletedOnboarding,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            return Ok(dto);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.RefreshToken))
                throw new ArgumentException("Refresh token zorunludur.");

            var session = await _sessionService.GetByRefreshTokenAsync(request.RefreshToken);
            if (session == null || session.RefreshTokenExpiresAt < DateTime.UtcNow)
                throw new ArgumentException("Geçersiz veya süresi dolmuş refresh token.");

            var user = await _userService.GetByIdAsync(session.UserId);
            if (user == null)
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            var newAccessToken = GenerateJwtToken(user);
            var newAccessExp = DateTime.UtcNow.AddMinutes(30);

            session.AccessToken = newAccessToken;
            session.AccessTokenExpiresAt = newAccessExp;
            await _sessionService.UpdateAsync(session);

            var dto = new AuthResultDto
            {
                AccessToken = newAccessToken,
                AccessTokenExpiresAt = newAccessExp,
                RefreshToken = session.RefreshToken,
                RefreshTokenExpiresAt = (DateTime)session.RefreshTokenExpiresAt,
                UserId = user.UserId,
                HasCompletedOnboarding = user.HasCompletedOnboarding,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            return Ok(dto);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // server-side invalidation eklenecekse burada yapılır.
            return Ok(true); // bool dön; wrapper ApiResponse<bool>.Ok yapar
        }

        // ===== helpers =====

        private async Task<(string accessToken, string refreshToken, DateTime accessExp, DateTime refreshExp)> IssueSessionAsync(User user)
        {
            var accessToken = GenerateJwtToken(user);
            var refreshToken = Guid.NewGuid().ToString();
            var accessExp = DateTime.UtcNow.AddMinutes(30);
            var refreshExp = DateTime.UtcNow.AddDays(7);

            await _sessionService.CreateAsync(new Session
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessExp,
                RefreshTokenExpiresAt = refreshExp
            });

            return (accessToken, refreshToken, accessExp, refreshExp);
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
