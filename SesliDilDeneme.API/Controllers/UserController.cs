using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ProgressService _progressService;
        private readonly IRepository<Progress> _progressRepository;

        public UserController(UserService userService, ProgressService progressService,IRepository<Progress> progressRepository)
        {
            _userService = userService;
            _progressService = progressService;
            _progressRepository = progressRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid Id");
            var user = await _userService.GetByIdAsync<string>(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] UserDto userDto)
        {
            if (userDto == null) return BadRequest("Invalid User Data");
            if (string.IsNullOrEmpty(userDto.SocialProvider)) return BadRequest("SocialProvider is required.");
            if (string.IsNullOrEmpty(userDto.SocialId)) return BadRequest("SocialId is required.");
            if (string.IsNullOrEmpty(userDto.FirstName)) return BadRequest("FirstName is required.");
            if (string.IsNullOrEmpty(userDto.LastName)) return BadRequest("LastName is required.");
            if (string.IsNullOrEmpty(userDto.NativeLanguage)) userDto.NativeLanguage = "en";
            if (string.IsNullOrEmpty(userDto.TargetLanguage)) userDto.TargetLanguage = "en";

            var user = new User
            {
                SocialProvider = userDto.SocialProvider.Length > 10 ? userDto.SocialProvider[..10] : userDto.SocialProvider,
                SocialId = userDto.SocialId.Length > 255 ? userDto.SocialId[..255] : userDto.SocialId,
                Email = userDto.Email?.Length > 255 ? userDto.Email[..255] : userDto.Email,
                FirstName = userDto.FirstName.Length > 100 ? userDto.FirstName[..100] : userDto.FirstName,
                LastName = userDto.LastName.Length > 100 ? userDto.LastName[..100] : userDto.LastName,
                NativeLanguage = userDto.NativeLanguage.Length > 10 ? userDto.NativeLanguage[..10] : userDto.NativeLanguage,
                TargetLanguage = userDto.TargetLanguage.Length > 10 ? userDto.TargetLanguage[..10] : userDto.TargetLanguage,
                ProficiencyLevel = userDto.ProficiencyLevel?.Length > 2 ? userDto.ProficiencyLevel[..2] : userDto.ProficiencyLevel,
                AgeRange = userDto.AgeRange?.Length > 5 ? userDto.AgeRange[..5] : userDto.AgeRange,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                LearningGoals = JsonDocument.Parse(JsonSerializer.Serialize(userDto.LearningGoals ?? Array.Empty<string>())),
                ImprovementGoals = JsonDocument.Parse("[]"),
                TopicInterests = JsonDocument.Parse("[]"),
                WeeklySpeakingGoal = ""
            };

            try
            {
                await _userService.CreateAsync(user);
                return CreatedAtAction(nameof(GetById), new { id = user.UserId }, user);
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = "Database error while saving changes", error = innerException });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UserDto userDto)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");
            if (userDto == null) return BadRequest("Invalid User Data");
            if (string.IsNullOrEmpty(userDto.NativeLanguage)) userDto.NativeLanguage = "en";
            if (string.IsNullOrEmpty(userDto.TargetLanguage)) userDto.TargetLanguage = "en";

            var user = await _userService.GetByIdAsync<string>(id);
            if (user == null) return NotFound();

            user.SocialProvider = userDto.SocialProvider?.Length > 10 ? userDto.SocialProvider[..10] : userDto.SocialProvider;
            user.SocialId = userDto.SocialId?.Length > 255 ? userDto.SocialId[..255] : userDto.SocialId;
            user.Email = userDto.Email?.Length > 255 ? userDto.Email[..255] : userDto.Email;
            user.FirstName = userDto.FirstName?.Length > 100 ? userDto.FirstName[..100] : userDto.FirstName;
            user.LastName = userDto.LastName?.Length > 100 ? userDto.LastName[..100] : userDto.LastName;
            user.NativeLanguage = userDto.NativeLanguage.Length > 10 ? userDto.NativeLanguage[..10] : userDto.NativeLanguage;
            user.TargetLanguage = userDto.TargetLanguage.Length > 10 ? userDto.TargetLanguage[..10] : userDto.TargetLanguage;
            user.ProficiencyLevel = userDto.ProficiencyLevel?.Length > 2 ? userDto.ProficiencyLevel[..2] : userDto.ProficiencyLevel;
            user.AgeRange = userDto.AgeRange?.Length > 5 ? userDto.AgeRange[..5] : userDto.AgeRange;
            user.LearningGoals = JsonDocument.Parse(JsonSerializer.Serialize(userDto.LearningGoals ?? Array.Empty<string>()));
            user.ImprovementGoals = JsonDocument.Parse(JsonSerializer.Serialize(userDto.ImprovementGoals ?? Array.Empty<string>()));
            user.TopicInterests = JsonDocument.Parse(JsonSerializer.Serialize(userDto.TopicInterests ?? Array.Empty<string>()));
            user.WeeklySpeakingGoal = userDto.WeeklySpeakingGoal ?? "";

            user.LastLoginAt = DateTime.UtcNow;

            try
            {
                await _userService.UpdateAsync(user);
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = "Database error while saving changes", error = innerException });
            }
        }

        [HttpPost("social")]
        public async Task<ActionResult<UserDto>> CreateOrUpdateBySocial([FromBody] UserDto userDto)
        {
            if (userDto == null || string.IsNullOrEmpty(userDto.SocialProvider) || string.IsNullOrEmpty(userDto.SocialId))
                return BadRequest("Invalid data: SocialProvider and SocialId are required.");
            if (string.IsNullOrEmpty(userDto.FirstName)) userDto.FirstName = "User";
            if (string.IsNullOrEmpty(userDto.LastName)) userDto.LastName = "LastName";
            if (string.IsNullOrEmpty(userDto.NativeLanguage)) userDto.NativeLanguage = "en";
            if (string.IsNullOrEmpty(userDto.TargetLanguage)) userDto.TargetLanguage = "en";

            try
            {
                var user = await _userService.GetOrCreateBySocialAsync(
                    userDto.SocialProvider,
                    userDto.SocialId,
                    userDto.Email,
                    userDto.FirstName,
                    userDto.LastName
                );
                return Ok(user);
            }

            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = "Database error while saving changes", error = innerException });
            }
        }

        [HttpPost("onboarding/{userId}")]
        public async Task<IActionResult> Onboarding(string userId, [FromBody] OnboardingDto onboardingDto)
        {
            try
            {
                if (onboardingDto == null)
                    return BadRequest("Geçersiz onboarding verileri");

                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                    return NotFound("Kullanıcı bulunamadı");

                user.NativeLanguage = onboardingDto.NativeLanguage;
                user.TargetLanguage = onboardingDto.TargetLanguage;
                user.ProficiencyLevel = onboardingDto.ProficiencyLevel;
                user.AgeRange = onboardingDto.AgeRange;
                user.HasCompletedOnboarding = true;
                user.LearningGoals = JsonDocument.Parse(JsonSerializer.Serialize(onboardingDto.LearningGoals ?? Array.Empty<string>()));
                user.ImprovementGoals = JsonDocument.Parse(JsonSerializer.Serialize(onboardingDto.ImprovementGoals ?? Array.Empty<string>()));
                user.TopicInterests = JsonDocument.Parse(JsonSerializer.Serialize(onboardingDto.TopicInterests ?? Array.Empty<string>()));
                user.WeeklySpeakingGoal = onboardingDto.WeeklySpeakingGoal ?? "";
                user.LastLoginAt = DateTime.UtcNow;

                // Kullanıcıyı kaydet
                await _userService.UpdateAsync(user);

                var progress = await _progressService.GetSingleByUserIdAsync(userId);
                if (progress == null)
                {
                    progress = new Progress
                    {
                        ProgressId = Guid.NewGuid().ToString(),
                        UserId = userId,
                        CurrentLevel = user.ProficiencyLevel ?? "A1",
                        DailyConversationCount = 0,
                        TotalConversationTimeMinutes = 0,
                        CurrentStreakDays = 0,
                        LongestStreakDays = 0,
                        LastConversationDate = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        BestWordsPerMinute = 0.0

                    };
                    await _progressRepository.AddAsync(progress);
                    await _progressRepository.SaveChangesAsync();
                }
                else
                {
                    progress.CurrentLevel = user.ProficiencyLevel ?? "A1";
                    progress.UpdatedAt = DateTime.UtcNow;
                    _progressRepository.Update(progress);
                    await _progressRepository.SaveChangesAsync();
                }

                return Ok("Onboarding bilgileri ve ilerleme kaydedildi.");
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"[ONBOARDING HATASI]: {ex.Message}, İç Hata: {innerException}");
                return StatusCode(500, $"Sunucu hatası: {innerException}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ONBOARDING HATASI]: {ex.Message}");
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }
        [HttpPatch("{userId}/preferences")]
        public async Task<IActionResult> UpdatePreferences(string userId, [FromBody] UpdateUserPreferencesDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Invalid preferences data.");

                // Helper method ile kontrol ve dönüşüm yapabiliriz
                JsonDocument? ValidateAndFix(JsonDocument? doc, string fieldName)
                {
                    if (doc == null) return null;

                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                        return doc; // Doğru

                    if (root.ValueKind == JsonValueKind.String)
                    {
                        // Tek string gelmiş, bunu dizi haline çevirelim
                        var singleItem = new List<string> { root.GetString() ?? "" };
                        var jsonStr = JsonSerializer.Serialize(singleItem);
                        return JsonDocument.Parse(jsonStr);
                    }

                    throw new ArgumentException($"{fieldName} should be a JSON array or string.");
                }

                try
                {
                    dto.LearningGoals = ValidateAndFix(dto.LearningGoals, "learningGoals");
                    dto.ImprovementGoals = ValidateAndFix(dto.ImprovementGoals, "improvementGoals");
                    dto.TopicInterests = ValidateAndFix(dto.TopicInterests, "topicInterests");
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }

                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                    return NotFound("User not found.");

                user.LearningGoals = dto.LearningGoals ?? user.LearningGoals;
                user.ImprovementGoals = dto.ImprovementGoals ?? user.ImprovementGoals;
                user.TopicInterests = dto.TopicInterests ?? user.TopicInterests;
                user.WeeklySpeakingGoal = dto.WeeklySpeakingGoal ?? user.WeeklySpeakingGoal;

                await _userService.UpdateAsync(user);

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"[UPDATE PREFERENCES ERROR]: {ex.Message}, Inner: {innerException}");
                return StatusCode(500, $"Server error: {innerException}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UPDATE PREFERENCES ERROR]: {ex.Message}");
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }
        [HttpDelete("{id}/full-delete")]
        public async Task<IActionResult> DeleteUserCompletely(string id)
        {
            var result = await _userService.DeleteUserCompletelyAsync(id);
            if (!result)
                return NotFound(new { message = "User not found." });

            return Ok(new { message = "User and all related data deleted successfully." });
        }

    
    }
}
