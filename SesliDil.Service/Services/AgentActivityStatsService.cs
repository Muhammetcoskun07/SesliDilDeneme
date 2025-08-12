using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.DTOs;
using SesliDil.Data.Context;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class AgentActivityStatsService:IAgentActivityStatsService
    {
        private readonly SesliDilDbContext _context;
        public AgentActivityStatsService(SesliDilDbContext context)
        {
            _context = context;
        }
        public async Task<UserAgentStatsDto> GetAgentStatsForUserAsync(string userId, string agentId)
        {
            var activities = await _context.ConversationAgentActivities
                .Where(a => a.UserId == userId && a.AgentId == agentId)
                .ToListAsync();

            if (!activities.Any())
                return new UserAgentStatsDto();

            return new UserAgentStatsDto
            {
                AgentId = agentId,
                TotalMinutes = activities.Sum(a => a.Duration.TotalMinutes),
                TotalMessages = activities.Sum(a => a.MessageCount),
                TotalWords = activities.Sum(a => a.WordCount),
                AverageWPM = activities.Average(a => a.WordsPerMinute)
            };
        }

        public async Task<List<UserAgentStatsDto>> GetUserAllAgentStatsAsync(string userId)
        {
            var query = await _context.ConversationAgentActivities
                .Where(a => a.UserId == userId)
                .GroupBy(a => a.AgentId)
                .Select(g => new UserAgentStatsDto
                {
                    AgentId = g.Key,
                    TotalMinutes = g.Sum(x => x.Duration.TotalMinutes),
                    TotalMessages = g.Sum(x => x.MessageCount),
                    TotalWords = g.Sum(x => x.WordCount),
                    AverageWPM = g.Average(x => x.WordsPerMinute)
                })
                .ToListAsync();

            return query;
        }


    }
}
