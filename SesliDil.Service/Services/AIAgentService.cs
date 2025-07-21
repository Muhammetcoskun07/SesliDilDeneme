using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class AIAgentService : Service<AIAgent>, IService<AIAgent>
    {
        private readonly IRepository<AIAgent> _agentRepository;
        private readonly IMapper _mapper;

        public AIAgentService(IRepository<AIAgent> agentRepository, IMapper mapper)
            : base(agentRepository, mapper)
        {
            _agentRepository = agentRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AIAgentDto>> GetActiveAgentsAsync()
        {
            var agents = await _agentRepository.GetAllAsync();
            var activeAgents = agents.Where(a => a.IsActive);
            return _mapper.Map<IEnumerable<AIAgentDto>>(activeAgents);
        }

        public async Task<AIAgentDto> GetByTypeAsync(string agentType)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentNullException("Invalid agent type", nameof(agentType));

            var agent = (await _agentRepository.GetAllAsync())
                        .FirstOrDefault(a => a.AgentType.ToLower() == agentType.ToLower() && a.IsActive);

            return _mapper.Map<AIAgentDto>(agent);
        }
    }
}
