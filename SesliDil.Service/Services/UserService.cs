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

        public async Task<User> GetOrCreateBySocialAsync(string socialProvider, string socialId, string email, string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(socialProvider) || string.IsNullOrEmpty(socialId))
            {
                throw new ArgumentNullException("Social provider and social ID are required");
            }

            // Email, firstName, lastName boşsa default değer ata (örneğin boş string değil, güvenli bir değer)
            email = string.IsNullOrEmpty(email) ? $"{socialId}@{socialProvider}.local" : email;
            firstName = string.IsNullOrEmpty(firstName) ? $"{socialProvider}_User" : firstName;
            lastName = string.IsNullOrEmpty(lastName) ? "LastName" : lastName;

            // Daha performanslı ve doğrudan sorgu yapalım
            var existingUser = await _userRepository.Query()
                .FirstOrDefaultAsync(u => u.SocialProvider == socialProvider && u.SocialId == socialId);

            if (existingUser != null)
            {
                // Giriş yapma sırasında last login güncelle
                existingUser.LastLoginAt = DateTime.UtcNow;
                await UpdateAsync(existingUser);
                return existingUser;
            }

            var newUser = new User
            {
                UserId = Guid.NewGuid().ToString(),
                SocialProvider = socialProvider,
                SocialId = socialId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                NativeLanguage = null,
                TargetLanguage = null,
                ProficiencyLevel = null,
                AgeRange = null,
                LearningGoals = JsonDocument.Parse("[]"),
                Hobbies = JsonDocument.Parse("[]"),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };


            try
            {
                await CreateAsync(newUser);
                return newUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"User creation failed: {ex.ToString()}");
                throw;
            }
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


    }
}
