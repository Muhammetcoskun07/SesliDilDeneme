using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Data.Context;
using System.Text.Json;

namespace SesliDil.Service.Services
{
    public class UserService : Service<User>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IMapper _mapper;
        private readonly SesliDilDbContext _context;

        public UserService(IRepository<User> repository, IMapper mapper, SesliDilDbContext context)
            : base(repository, mapper)
        {
            _userRepository = repository;
            _mapper = mapper;
            _context = context;
        }

        // ---------- Ortak yardımcılar ----------
        public async Task<User> GetByIdOrThrowAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz id.");

            var user = await GetByIdAsync<string>(id);
            if (user is null)
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            return user;
        }

        private static string Trunc(string? s, int max)
            => string.IsNullOrEmpty(s) ? s ?? "" : (s.Length > max ? s[..max] : s);

        private static JsonDocument ToJsonArray(IEnumerable<string>? items)
            => JsonDocument.Parse(JsonSerializer.Serialize(items ?? Array.Empty<string>()));

        private static JsonDocument? ValidateAndFix(JsonDocument? doc, string fieldName)
        {
            if (doc is null) return null;
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array) return doc;
            if (root.ValueKind == JsonValueKind.String)
            {
                var single = new List<string> { root.GetString() ?? "" };
                return JsonDocument.Parse(JsonSerializer.Serialize(single));
            }

            throw new ArgumentException($"{fieldName} bir JSON array ya da string olmalıdır.");
        }

        // ---------- Create ----------
        public async Task<User> CreateFromDtoAsync(UserDto dto)
        {
            if (dto is null) throw new ArgumentException("Kullanıcı verisi zorunludur.");
            if (string.IsNullOrWhiteSpace(dto.SocialProvider)) throw new ArgumentException("SocialProvider zorunludur.");
            if (string.IsNullOrWhiteSpace(dto.SocialId)) throw new ArgumentException("SocialId zorunludur.");
            if (string.IsNullOrWhiteSpace(dto.FirstName)) throw new ArgumentException("FirstName zorunludur.");
            if (string.IsNullOrWhiteSpace(dto.LastName)) throw new ArgumentException("LastName zorunludur.");

            dto.NativeLanguage ??= "en";
            dto.TargetLanguage ??= "en";

            var user = new User
            {
                SocialProvider = Trunc(dto.SocialProvider, 10),
                SocialId = Trunc(dto.SocialId, 255),
                Email = string.IsNullOrEmpty(dto.Email) ? null : Trunc(dto.Email, 255),
                FirstName = Trunc(dto.FirstName, 100),
                LastName = Trunc(dto.LastName, 100),
                NativeLanguage = Trunc(dto.NativeLanguage, 10),
                TargetLanguage = Trunc(dto.TargetLanguage, 10),
                ProficiencyLevel = dto.ProficiencyLevel is null ? null : Trunc(dto.ProficiencyLevel, 2),
                AgeRange = dto.AgeRange is null ? null : Trunc(dto.AgeRange, 5),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                LearningGoals = ToJsonArray(dto.LearningGoals),
                ImprovementGoals = JsonDocument.Parse("[]"),
                TopicInterests = JsonDocument.Parse("[]"),
                WeeklySpeakingGoal = ""
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return user;
        }

        // ---------- Update ----------
        public async Task<User> UpdateFromDtoAsync(string id, UserUpdateDto dto)
        {
            if (dto is null) throw new ArgumentException("Kullanıcı verisi zorunludur.");

            var user = await GetByIdOrThrowAsync(id);

            user.TargetLanguage = Trunc(dto.TargetLanguage, 10);
            user.ProficiencyLevel = dto.ProficiencyLevel is null ? null : Trunc(dto.ProficiencyLevel, 2);
            user.LearningGoals = ToJsonArray(dto.LearningGoals);
            user.ImprovementGoals = ToJsonArray(dto.ImprovementGoals);
            user.TopicInterests = ToJsonArray(dto.TopicInterests);
            user.WeeklySpeakingGoal = dto.WeeklySpeakingGoal ?? "";

            await UpdateAsync(user);
            return user;
        }

        // ---------- Social Create/Update (controller’dan validasyon service’e) ----------
        public async Task<User> CreateOrUpdateBySocialValidatedAsync(UserDto dto)
        {
            if (dto is null ||
                string.IsNullOrWhiteSpace(dto.SocialProvider) ||
                string.IsNullOrWhiteSpace(dto.SocialId))
                throw new ArgumentException("SocialProvider ve SocialId zorunludur.");

            dto.FirstName ??= "User";
            dto.LastName ??= "LastName";
            dto.NativeLanguage ??= "en";
            dto.TargetLanguage ??= "en";

            return await GetOrCreateBySocialAsync(
                dto.SocialProvider,
                dto.SocialId,
                dto.Email ?? $"{dto.SocialId}@{dto.SocialProvider.ToLower()}.local",
                dto.FirstName,
                dto.LastName
            );
        }

        // Mevcut senin metodun (ufak dokunuş yok, aynen kalsın)
        public async Task<User> GetOrCreateBySocialAsync(string provider, string socialId, string email, string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(provider))
                throw new ArgumentException("SocialProvider cannot be null or empty.");
            if (string.IsNullOrEmpty(socialId))
                throw new ArgumentException("SocialId cannot be null or empty.");

            if (string.IsNullOrEmpty(email))
                email = $"{socialId}@{provider.ToLower()}.local";

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.SocialProvider == provider && u.SocialId == socialId);

            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return user;
            }

            user = new User
            {
                SocialProvider = Trunc(provider, 10),
                SocialId = Trunc(socialId, 255),
                Email = Trunc(email, 255),
                FirstName = string.IsNullOrWhiteSpace(firstName) ? "" : Trunc(firstName, 100),
                LastName = string.IsNullOrWhiteSpace(lastName) ? "" : Trunc(lastName, 100),
                NativeLanguage = "en",
                TargetLanguage = "en",
                ProficiencyLevel = "A1",
                AgeRange = "18-24",
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                LearningGoals = JsonDocument.Parse("[]"),
                ImprovementGoals = JsonDocument.Parse("[]"),
                TopicInterests = JsonDocument.Parse("[]"),
                WeeklySpeakingGoal = "",
            };

            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Failed to save user: {innerException}", ex);
            }

            return user;
        }

        // ---------- Onboarding ----------
        public async Task OnboardingAsync(string userId, OnboardingDto onboardingDto)
        {
            if (onboardingDto is null)
                throw new ArgumentException("Onboarding verisi zorunludur.");

            var user = await GetByIdOrThrowAsync(userId);

            user.NativeLanguage = onboardingDto.NativeLanguage;
            user.TargetLanguage = onboardingDto.TargetLanguage;
            user.ProficiencyLevel = onboardingDto.ProficiencyLevel;
            user.AgeRange = onboardingDto.AgeRange;
            user.HasCompletedOnboarding = true;
            user.LearningGoals = ToJsonArray(onboardingDto.LearningGoals);
            user.ImprovementGoals = ToJsonArray(onboardingDto.ImprovementGoals);
            user.TopicInterests = ToJsonArray(onboardingDto.TopicInterests);
            user.WeeklySpeakingGoal = onboardingDto.WeeklySpeakingGoal ?? "";
            user.LastLoginAt = DateTime.UtcNow;

            await UpdateAsync(user);

            // Progress oluştur/güncelle
            var progress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (progress is null)
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

                await _context.Progresses.AddAsync(progress);
                await _context.SaveChangesAsync();
            }
            else
            {
                progress.CurrentLevel = user.ProficiencyLevel ?? "A1";
                progress.UpdatedAt = DateTime.UtcNow;
                _context.Progresses.Update(progress);
                await _context.SaveChangesAsync();
            }
        }

        // ---------- Preferences ----------
        public async Task<User> UpdatePreferencesAsync(string userId, UpdateUserPreferencesDto dto)
        {
            if (dto is null)
                throw new ArgumentException("Preferences verisi zorunludur.");

            dto.LearningGoals = ValidateAndFix(dto.LearningGoals, "learningGoals");
            dto.ImprovementGoals = ValidateAndFix(dto.ImprovementGoals, "improvementGoals");
            dto.TopicInterests = ValidateAndFix(dto.TopicInterests, "topicInterests");

            var user = await GetByIdOrThrowAsync(userId);

            user.LearningGoals = dto.LearningGoals ?? user.LearningGoals;
            user.ImprovementGoals = dto.ImprovementGoals ?? user.ImprovementGoals;
            user.TopicInterests = dto.TopicInterests ?? user.TopicInterests;
            user.WeeklySpeakingGoal = dto.WeeklySpeakingGoal ?? user.WeeklySpeakingGoal;

            await UpdateAsync(user);
            return user;
        }

        // ---------- Senin mevcut metodların (dokunmadım / küçük iyileştirmelerle korudum) ----------
        public async Task<bool> DeleteUserCompletelyAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.Messages
                    .Where(m => _context.Conversations
                        .Where(c => c.UserId == userId)
                        .Select(c => c.ConversationId)
                        .Contains(m.ConversationId))
                    .ExecuteDeleteAsync();

                await _context.Conversations
                    .Where(c => c.UserId == userId)
                    .ExecuteDeleteAsync();

                await _context.Sessions
                    .Where(s => s.UserId == userId)
                    .ExecuteDeleteAsync();

                await _context.Progresses
                    .Where(p => p.UserId == userId)
                    .ExecuteDeleteAsync();

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateLearningGoalsAsync(string userId, List<string> goals)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.LearningGoals = JsonDocument.Parse(JsonSerializer.Serialize(goals));
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User> UpdateProfileAsync(string userId, string? firstName, string? lastName)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            bool updated = false;

            if (!string.IsNullOrWhiteSpace(firstName) && user.FirstName != firstName)
            {
                user.FirstName = Trunc(firstName, 100);
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(lastName) && user.LastName != lastName)
            {
                user.LastName = Trunc(lastName, 100);
                updated = true;
            }

            if (updated)
            {
                await UpdateAsync(user);
            }

            return user;
        }

        public async Task UpdateProficiencyLevelAsync(string userId, string newLevel)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            user.ProficiencyLevel = MapToCEFR(newLevel);
            await _context.SaveChangesAsync();
        }

        private string MapToCEFR(string level)
        {
            return level switch
            {
                "Beginner" => "A1  I know basic words and simple phrases",
                "Developing" => "A2  I can carry on basic conversations",
                "Intermediate" => "B1  I know basic words and simple phrases",
                "Advanced" => "B2  I can discuss various topics with ease",
                "Fluent" => "C1  I speak confidently in complex situations",
                "Native" => "C2  I speak like a native in all contexts",
                _ => null
            };
        }
    }
}
