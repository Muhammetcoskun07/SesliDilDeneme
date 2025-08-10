using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Core.Mappings;
using SesliDil.Data.Context;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class ConversationService : Service<Conversation>, IService<Conversation>
    {
        private readonly IRepository<Conversation> _conversationRepository;
        private readonly IMapper _mapper;
        private readonly SesliDilDbContext _dbContext;

        public ConversationService(IRepository<Conversation> conversationRepository, IMapper mapper,SesliDilDbContext dbContext)
            : base(conversationRepository, mapper)
        {
            _dbContext = dbContext;
            _conversationRepository = conversationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ConversationDto>> GetByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("Invalid User");

            var conversations = await _conversationRepository.GetAllAsync();
            var userConversations = conversations.Where(c => c.UserId == userId);

            return _mapper.Map<IEnumerable<ConversationDto>>(userConversations);
        }

        public async Task<ConversationSummaryDto> GetSummaryByConversationIdAsync(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
                throw new ArgumentException("Invalid Conversation Id");

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                throw new Exception("Conversation not found");

            return _mapper.Map<ConversationSummaryDto>(conversation);

  
        }
        public async Task<string> SaveAgentActivityAsync(string conversationId, string userId, string agentId, TimeSpan duration, int messageCount)
        {
            var activity = new ConversationAgentActivity
            {
                ActivityId = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                UserId = userId,
                AgentId = agentId,
                Duration = duration,
                MessageCount = messageCount,
              //  CreatedAt = DateTime.UtcNow
            };
            await _dbContext.ConversationAgentActivities.AddAsync(activity);
            await _dbContext.SaveChangesAsync();
            return activity.ActivityId;
        }
        public async Task SaveSummaryAsync(string conversationId, string summary)
        {
            var conv = await _conversationRepository.GetByIdAsync(conversationId);
            if (conv == null) throw new Exception("Conversation bulunamadı");

            conv.Summary = summary;
            _conversationRepository.Update(conv);            
            await _conversationRepository.SaveChangesAsync();
        }


        public async Task EndConversationAsync(string conversationId)
        {
            var conversation = await GetByIdAsync<string>(conversationId);
            if (conversation == null)
                throw new ArgumentException("Conversation not found");

            var duration = DateTime.UtcNow - conversation.StartedAt;
            conversation.DurationMinutes = duration.TotalMinutes;
            await UpdateAsync(conversation);
        }
    }
}
