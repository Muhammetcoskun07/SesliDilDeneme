using SesliDil.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Service.Interfaces
{
    public interface IUserService : IService<User>
    {
        Task<IEnumerable<User>> GetByGenderAsync(string gender);
        Task<IEnumerable<User>> GetByInterestAsync(string interest);
        Task<IEnumerable<User>> GetRecentRegistrationsAsync(int days);
        Task<int> GetAverageAgeAsync();
    }
}
