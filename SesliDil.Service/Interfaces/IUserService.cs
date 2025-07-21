using SesliDil.Core.DTOs;
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
        Task<User> GetOrCreateBySocialAsync();
    }
}
