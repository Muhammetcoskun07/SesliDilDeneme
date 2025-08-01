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
            // Veri doğrulama
            if (string.IsNullOrEmpty(provider))
                throw new ArgumentException("SocialProvider cannot be null or empty.");
            if (string.IsNullOrEmpty(socialId))
                throw new ArgumentException("SocialId cannot be null or empty.");
            if (string.IsNullOrEmpty(firstName))
                throw new ArgumentException("FirstName cannot be null or empty.");
            if (string.IsNullOrEmpty(lastName))
                throw new ArgumentException("LastName cannot be null or empty.");
            if (string.IsNullOrEmpty(email))
                email = $"{socialId}@{provider.ToLower()}.local"; // Varsayılan e-posta

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.SocialProvider == provider && u.SocialId == socialId);

            if (user != null)
                return user;

            user = new User
            {
                SocialProvider = provider.Length > 10 ? provider.Substring(0, 10) : provider,
                SocialId = socialId.Length > 255 ? socialId.Substring(0, 255) : socialId,
                Email = email?.Length > 255 ? email.Substring(0, 255) : email,
                FirstName = firstName.Length > 100 ? firstName.Substring(0, 100) : firstName,
                LastName = lastName.Length > 100 ? lastName.Substring(0, 100) : lastName,
                NativeLanguage = "en", // Varsayılan dil
                TargetLanguage = "en", // Varsayılan hedef dil
                ProficiencyLevel = "A1", // Varsayılan yeterlilik seviyesi
                AgeRange = "18-24", // Varsayılan yaş aralığı
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                LearningGoals = JsonDocument.Parse("[]"),
                Hobbies = JsonDocument.Parse("[]")
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

            // 1. Kullanıcının tüm session'larını sil
            var sessions = await _context.Sessions.Where(s => s.UserId == userId).ToListAsync();
            _context.Sessions.RemoveRange(sessions);

            // 2. Kullanıcının progress kayıtlarını sil
            var progresses = await _context.Progresses.Where(p => p.UserId == userId).ToListAsync();
            _context.Progresses.RemoveRange(progresses);

            // 3. Kullanıcının konuşmalarını bul
            var conversations = await _context.Conversations.Where(c => c.UserId == userId).ToListAsync();

            foreach (var convo in conversations)
            {
                // 3.1 Konuşmanın mesajlarını sil
                var messages = await _context.Messages.Where(m => m.ConversationId == convo.ConversationId).ToListAsync();
                _context.Messages.RemoveRange(messages);

                // 3.2 Konuşmaya ait dosyaları sil
                var files = await _context.FileStorages.Where(f => f.ConversationId == convo.ConversationId).ToListAsync();
                _context.FileStorages.RemoveRange(files);
            }

            // 4. Kullanıcının konuşmalarını sil
            _context.Conversations.RemoveRange(conversations);

            // 5. Kullanıcının doğrudan yüklediği dosyaları da sil (konuşmaya bağlı olmayanlar varsa)
            var userFiles = await _context.FileStorages.Where(f => f.UserId == userId).ToListAsync();
            _context.FileStorages.RemoveRange(userFiles);

            // 6. Kullanıcının kendisini sil
            _context.Users.Remove(user);

            // 7. Kaydet
            await _context.SaveChangesAsync();
            return true;
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

    }
}
