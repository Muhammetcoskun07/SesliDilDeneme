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
        public async Task<AIAgentDto> CreateAgentAsync(AIAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent), "Agent verisi zorunludur.");

            agent.AgentId = Guid.NewGuid().ToString(); // ID ataması

            await _agentRepository.AddAsync(agent);
            await _agentRepository.SaveChangesAsync(); // kaydetmeyi unutma

            return _mapper.Map<AIAgentDto>(agent); // DTO ile dön
        }
        public async Task<AIAgent> UpdateAgentAsync(string id, AIAgent updatedAgent)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz agent id.");
            if (updatedAgent == null)
                throw new ArgumentNullException(nameof(updatedAgent), "Güncelleme verisi zorunludur.");

            var agent = await _agentRepository.GetByIdAsync(id);
            if (agent == null)
                throw new KeyNotFoundException("Agent bulunamadı.");

            agent.AgentName = updatedAgent.AgentName;
            agent.AgentPrompt = updatedAgent.AgentPrompt;
            agent.AgentDescription = updatedAgent.AgentDescription;
            agent.AgentType = updatedAgent.AgentType;
            agent.IsActive = updatedAgent.IsActive;

            await _agentRepository.UpdateAsync(agent);
            return agent;
        }
        public async Task<string> DeleteAgentAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Geçersiz agent id.");

            var agent = await _agentRepository.GetByIdAsync(id);
            if (agent == null)
                throw new KeyNotFoundException("Agent bulunamadı.");

            _agentRepository.Delete(agent);      // Delete metodu senin interface’ten
            await _agentRepository.SaveChangesAsync(); // değişiklikleri kaydet

            return id;
        }
    }
}
