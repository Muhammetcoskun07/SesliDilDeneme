using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SesliDil.Core.DTOs;

namespace SesliDil.Service.Interfaces
{
    public interface IAgentActivityStatsService
    {
        Task<List<UserAgentStatsDto>> GetUserAllAgentStatsAsync(string userId);
        Task<UserAgentStatsDto> GetAgentStatsForUserAsync(string userId, string agentId);
    }
}
