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
    public class ConversationService : Service<Conversation> , IService<Conversation>
    {
        private readonly IRepository<Conversation> _conversationRepository;
        private readonly IMapper _mapper;
        public ConversationService(IRepository<Conversation> conversationRepository, IMapper mapper)
     : base(conversationRepository, mapper)
        {
            _conversationRepository = conversationRepository;
            _mapper = mapper;
        }
        public async Task<ConversationDto> GetConversationByIdAsync(int threadId)
        {
            var conversation = await _conversationRepository.GetByIdAsync(threadId);
            if (conversation == null) throw new Exception("Conversation Not Found");
            return _mapper.Map<ConversationDto>(conversation);
        }
        public async Task<ConversationDto> GetConversationByThreadIdAsync(int threadId)
        {
            if(threadId==null) throw new ArgumentException("Invalid threadId");
            var conversation = await _conversationRepository.GetByIdAsync(threadId);
             return _mapper.Map<ConversationDto>(conversation);
        }
        public async Task<ConversationDto> StartConversationAsync(string agentId, string language)
        {
            if (string.IsNullOrEmpty(agentId) || string.IsNullOrEmpty(language)) throw new ArgumentException("Invalid Agent or Language");
            var conversation=new Conversation { ConversationId= Guid.NewGuid().ToString(), AgentId = agentId,
                Language = language, Status = "active", StartedAt = DateTime.UtcNow };
            await _conversationRepository.AddAsync(conversation);
            return _mapper.Map<ConversationDto>(conversation);
        }

        }
}
