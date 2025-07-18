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
    public class UserService : Service<User>, IUserService
    {
        private readonly IRepository<User> _userRepository;

        public UserService(IRepository<User> repository) : base(repository)
        {
            _userRepository = repository;
        }

        public async Task<IEnumerable<User>> GetByGenderAsync(string gender)
        {
            var users = await _userRepository.GetAllAsync();
            return users.Where(u => u.Gender == gender);
        }

        public async Task<IEnumerable<User>> GetByInterestAsync(string interest)
        {
            var users = await _userRepository.GetAllAsync();
            return users.Where(u => u.Interests.Contains(interest, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<User>> GetRecentRegistrationsAsync(int days)
        {
            var users = await _userRepository.GetAllAsync();
            var targetDate = DateTime.UtcNow.AddDays(-days);
            return users.Where(u => u.RegistrationDate >= targetDate);
        }

        public async Task<int> GetAverageAgeAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return (int)users.Average(u => u.Age);
        }
    }
}
