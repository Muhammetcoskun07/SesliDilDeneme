using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Core.Mappings;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class ConversationService : Service<Conversation>, IService<Conversation>
    {
        private readonly IRepository<Conversation> _conversationRepository;
        private readonly IMapper _mapper;

        public ConversationService(IRepository<Conversation> conversationRepository, IMapper mapper)
            : base(conversationRepository, mapper)
        {
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

            return new ConversationSummaryDto
            {
                ConversationId = conversation.ConversationId,
                Summary = conversation.Summary
            };
        }

        public async Task SaveSummaryAsync(string conversationId, string summary)
        {
            if (string.IsNullOrEmpty(conversationId))
                throw new ArgumentException("Invalid Conversation Id");

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                throw new Exception("Conversation not found");

            conversation.Summary = summary;
            conversation.LastUpdated = DateTime.UtcNow;
            await _conversationRepository.UpdateAsync(conversation);
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
