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
        public async Task<IEnumerable<Conversation>> GetByUserIdAsync(string userID)
        {
            if (string.IsNullOrEmpty(userID)) throw new ArgumentException("Invalid User");
            var conversations= await _conversationRepository.GetAllAsync();
            return conversations.Where(c=>c.UserId == userID);
        }
        

        }
}
