using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ProgressService _progressService;
        private readonly IRepository<Progress> _progressRepository;

        public UserController(
            UserService userService,
            ProgressService progressService,
            IRepository<Progress> progressRepository)
        {
            _userService = userService;
            _progressService = progressService;
            _progressRepository = progressRepository;
        }

        // GET: api/user
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        // GET: api/user/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var user = await _userService.GetByIdAsync<string>(id);
            if (user == null)
                throw new KeyNotFoundException();

            return Ok(user);
        }

        // POST: api/user
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserDto userDto)
        {
            if (userDto == null)
                throw new ArgumentException("Kullanıcı verisi zorunludur.");
            if (string.IsNullOrWhiteSpace(userDto.SocialProvider))
                throw new ArgumentException("SocialProvider zorunludur.");
            if (string.IsNullOrWhiteSpace(userDto.SocialId))
                throw new ArgumentException("SocialId zorunludur.");
            if (string.IsNullOrWhiteSpace(userDto.FirstName))
                throw new ArgumentException("FirstName zorunludur.");
            if (string.IsNullOrWhiteSpace(userDto.LastName))
                throw new ArgumentException("LastName zorunludur.");

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

            await _userService.CreateAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = user.UserId }, user);
        }

        // PUT: api/user/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UserUpdateDto userDto)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");
            if (userDto == null)
                throw new ArgumentException("Kullanıcı verisi zorunludur.");

            var user = await _userService.GetByIdAsync<string>(id);
            if (user == null)
                throw new KeyNotFoundException();

            user.TargetLanguage = userDto.TargetLanguage.Length > 10 ? userDto.TargetLanguage[..10] : userDto.TargetLanguage;
            user.ProficiencyLevel = userDto.ProficiencyLevel?.Length > 2 ? userDto.ProficiencyLevel[..2] : userDto.ProficiencyLevel;
            user.LearningGoals = JsonDocument.Parse(JsonSerializer.Serialize(userDto.LearningGoals ?? Array.Empty<string>()));
            user.ImprovementGoals = JsonDocument.Parse(JsonSerializer.Serialize(userDto.ImprovementGoals ?? Array.Empty<string>()));
            user.TopicInterests = JsonDocument.Parse(JsonSerializer.Serialize(userDto.TopicInterests ?? Array.Empty<string>()));
            user.WeeklySpeakingGoal = userDto.WeeklySpeakingGoal ?? "";

            await _userService.UpdateAsync(user);
            return Ok(user);
        }

        // POST: api/user/social
        [HttpPost("social")]
        public async Task<IActionResult> CreateOrUpdateBySocial([FromBody] UserDto userDto)
        {
            if (userDto == null ||
                string.IsNullOrWhiteSpace(userDto.SocialProvider) ||
                string.IsNullOrWhiteSpace(userDto.SocialId))
                throw new ArgumentException("SocialProvider ve SocialId zorunludur.");

            if (string.IsNullOrEmpty(userDto.FirstName)) userDto.FirstName = "User";
            if (string.IsNullOrEmpty(userDto.LastName)) userDto.LastName = "LastName";
            if (string.IsNullOrEmpty(userDto.NativeLanguage)) userDto.NativeLanguage = "en";
            if (string.IsNullOrEmpty(userDto.TargetLanguage)) userDto.TargetLanguage = "en";

            var user = await _userService.GetOrCreateBySocialAsync(
                userDto.SocialProvider,
                userDto.SocialId,
                userDto.Email,
                userDto.FirstName,
                userDto.LastName
            );

            return Ok(user);
        }

        // POST: api/user/onboarding/{userId}
        [HttpPost("onboarding/{userId}")]
        public async Task<IActionResult> Onboarding(string userId, [FromBody] OnboardingDto onboardingDto)
        {
            if (onboardingDto == null)
                throw new ArgumentException("Onboarding verisi zorunludur.");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException();

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

            return Ok(new { userId, hasCompletedOnboarding = true });
        }

        // PATCH: api/user/{userId}/preferences
        [HttpPatch("{userId}/preferences")]
        public async Task<IActionResult> UpdatePreferences(string userId, [FromBody] UpdateUserPreferencesDto dto)
        {
            if (dto == null)
                throw new ArgumentException("Preferences verisi zorunludur.");

            // Gelen string'i diziye çevirme desteği (mevcut mantık)
            JsonDocument? ValidateAndFix(JsonDocument? doc, string fieldName)
            {
                if (doc == null) return null;
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array) return doc;
                if (root.ValueKind == JsonValueKind.String)
                {
                    var singleItem = new List<string> { root.GetString() ?? "" };
                    return JsonDocument.Parse(JsonSerializer.Serialize(singleItem));
                }

                throw new ArgumentException($"{fieldName} bir JSON array ya da string olmalıdır.");
            }

            dto.LearningGoals = ValidateAndFix(dto.LearningGoals, "learningGoals");
            dto.ImprovementGoals = ValidateAndFix(dto.ImprovementGoals, "improvementGoals");
            dto.TopicInterests = ValidateAndFix(dto.TopicInterests, "topicInterests");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException();

            user.LearningGoals = dto.LearningGoals ?? user.LearningGoals;
            user.ImprovementGoals = dto.ImprovementGoals ?? user.ImprovementGoals;
            user.TopicInterests = dto.TopicInterests ?? user.TopicInterests;
            user.WeeklySpeakingGoal = dto.WeeklySpeakingGoal ?? user.WeeklySpeakingGoal;

            await _userService.UpdateAsync(user);
            return Ok(user);
        }
        [HttpPut("update-profile/{userId}")]
        public async Task<IActionResult> UpdateProfile([FromRoute] string userId, [FromBody] UpdateProfileRequest request)
        {
            try
            {
                var user = await _userService.UpdateProfileAsync(userId, request.FirstName, request.LastName, request.Email);

                // DTO yerine direkt anonim objeyle response dönebilirsin
                return Ok(new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.Email
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/user/{id}/full-delete
        [HttpDelete("{id}/full-delete")]
        public async Task<IActionResult> DeleteUserCompletely(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var result = await _userService.DeleteUserCompletelyAsync(id);
            if (!result)
                throw new KeyNotFoundException();

            return Ok(new { Deleted = true, Purged = true, Id = id });
        }
    }
}
