using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
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
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("UserId ve AgentId zorunludur.");

            var activities = await _context.ConversationAgentActivities
                .Where(a => a.UserId == userId && a.AgentId == agentId)
                .ToListAsync();

            if (activities == null || !activities.Any())
                throw new KeyNotFoundException("İlgili user/agent için stat bulunamadı.");

            return new UserAgentStatsDto
            {
                AgentId = agentId,
                TotalMinutes = activities.Sum(a => a.Duration.TotalMinutes),
                TotalMessages = activities.Sum(a => a.MessageCount),
                TotalWords = activities.Sum(a => a.WordCount),
                AverageWPM = activities.Average(a => a.WordsPerMinute),
                ConversationCount = activities.Select(a => a.ConversationId).Distinct().Count()
            };
        }

        public async Task<List<UserAgentStatsDto>> GetUserAllAgentStatsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Geçersiz userId.");

            var query = await _context.ConversationAgentActivities
                .Where(a => a.UserId == userId)
                .GroupBy(a => a.AgentId)
                .Select(g => new UserAgentStatsDto
                {
                    AgentId = g.Key,
                    TotalMinutes = g.Sum(x => x.Duration.TotalMinutes),
                    TotalMessages = g.Sum(x => x.MessageCount),
                    TotalWords = g.Sum(x => x.WordCount),
                    AverageWPM = g.Average(x => x.WordsPerMinute),
                    ConversationCount = g.Select(x => x.ConversationId).Distinct().Count()
                })
                .ToListAsync();

            if (query == null || !query.Any())
                throw new KeyNotFoundException("Kullanıcıya ait istatistik bulunamadı.");

            return query;
        }
        public async Task<List<ConversationAgentActivity>> GetByConversationIdAsync(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("Geçersiz conversationId.");

            var activities = await _context.ConversationAgentActivities
                .Where(a => a.ConversationId == conversationId)
                .ToListAsync();

            if (activities == null || activities.Count == 0)
                throw new KeyNotFoundException("Bu conversation için activity bulunamadı.");

            return activities;
        }


    }
}
