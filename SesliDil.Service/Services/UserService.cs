using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Service.Services
{
    public class UserService : Service<User>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IMapper _mapper;
        public UserService(IRepository<User> repository, IMapper mapper)
            : base(repository, mapper)
        {
            _userRepository = repository;
            _mapper = mapper;
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

    }
}
