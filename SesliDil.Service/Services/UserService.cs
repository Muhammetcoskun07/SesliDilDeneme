using AutoMapper;
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
        //private readonly IRepository<User> _userRepository;
        //private readonly IMapper _mapper;

        public UserService(IRepository<User> repository, IMapper mapper) : base(repository, mapper)
        {
        }
        public async Task<User> GetOrCreateBySocialAsync(string socialProvider, string socialId, string email, string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(socialProvider) || string.IsNullOrEmpty(socialId) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                throw new ArgumentNullException("Invalid social authentication data");
            }
            var existingUser = (await GetAllAsync()).FirstOrDefault(u => u.SocialProvider == socialProvider && u.SocialId == socialId);
            if (existingUser != null) return existingUser;
            var newUser = new User
            {
                UserId = Guid.NewGuid().ToString(),
                SocialProvider = socialProvider,
                SocialId = socialId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                NativeLanguage = null, //  kullanıcı tamamlayacak
                TargetLanguage = null, 
                ProficiencyLevel = null,
                AgeRange = null,
                CreatedAt =DateTime.Now,
                LastLoginAt=DateTime.Now
            };
            try
            {
                await CreateAsync(newUser);
                return newUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"User creation failed: {ex.Message}");
                throw; // Hata üst katmana iletilir
            }

        }


    }
}
