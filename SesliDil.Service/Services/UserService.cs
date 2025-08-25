using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Data.Context;
using SesliDil.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                // Mevcut kullanıcı varsa sadece login zamanını güncelle
                user.LastLoginAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return user;
            }

            // Yeni kullanıcı oluştur
            user = new User
            {
                SocialProvider = provider.Length > 10 ? provider[..10] : provider,
                SocialId = socialId.Length > 255 ? socialId[..255] : socialId,
                Email = email.Length > 255 ? email[..255] : email,
                FirstName = string.IsNullOrWhiteSpace(firstName) ? "" : (firstName.Length > 100 ? firstName[..100] : firstName),
                LastName = string.IsNullOrWhiteSpace(lastName) ? "" : (lastName.Length > 100 ? lastName[..100] : lastName),
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


        public async Task<bool> DeleteUserCompletelyAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            await using var transaction = await _context.Database.BeginTransactionAsync(); //kısmi silme olmayacak bu sayede

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
                    .ExecuteDeleteAsync(); //dbde delete sorgusunu çalıştırıyor

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
                user.FirstName = firstName.Length > 100 ? firstName[..100] : firstName;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(lastName) && user.LastName != lastName)
            {
                user.LastName = lastName.Length > 100 ? lastName[..100] : lastName;
                updated = true;
            }

            if (updated)
            {
                await UpdateAsync(user);
            }

            return user;
        }

    }
}
