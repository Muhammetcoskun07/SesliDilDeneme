using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class PromptService : IService<Prompt>
    {
        private readonly IRepository<Prompt> _promptRepository;

        public PromptService(IRepository<Prompt> promptRepository)
        {
            _promptRepository = promptRepository;
        }

        public async Task<IEnumerable<Prompt>> GetAllAsync()
        {
            return await _promptRepository.GetAllAsync();
        }

        public async Task<Prompt> GetByIdAsync<TId>(TId id)
        {
            return await _promptRepository.GetByIdAsync(id);
        }

        public async Task CreateAsync(Prompt entity)
        {
            await _promptRepository.AddAsync(entity);
            await _promptRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(Prompt entity)
        {
            await _promptRepository.UpdateAsync(entity);
            await _promptRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(Prompt entity)
        {
            _promptRepository.Delete(entity);
            await _promptRepository.SaveChangesAsync();
        }

        // Opsiyonel: AgentId'ye göre promptları getir
        public async Task<IEnumerable<Prompt>> GetByAgentAsync(string agentId)
        {
            return await _promptRepository.GetByAgentAsync(agentId);
        }
    
}
}
