using SesliDil.Core.Interfaces;
using SesliDil.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SesliDil.Core.Entities;

namespace SesliDil.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly SesliDilDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(SesliDilDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T> GetByIdAsync<TId>(TId id)
        {
            return await _dbSet.FindAsync(id); // <-- EF'nin native methodu, daha güvenli
        }


        public async Task<T> FindAsync(object id)
        {
            return await _context.Set<T>().FindAsync(id);
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }


        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }

        public Task UpdateAsync(Conversation conversation)
        {
            throw new NotImplementedException();
        }
        public async Task<Conversation> GetByIdWithMessagesAsync(string id)
        {
            return await _context.Conversations
                                 .Include(c => c.Messages)
                                 .FirstOrDefaultAsync(c => c.ConversationId == id);
        }
        public async Task<List<Message>> GetByConversationIdAsync(string conversationId)
        {
            // This assumes the repository is instantiated as Repository<Message>
            return await _dbSet
                .OfType<Message>() // Ensure T is Message, but since it's generic, use when T=Message
                .Where(m => m.ConversationId == conversationId)
                .ToListAsync();
        }
        public async Task<IEnumerable<T>> GetByAgentAsync(string agentId)
        {
            return await _dbSet
                .Where(e => EF.Property<string>(e, "AgentId") == agentId)
                .ToListAsync();
        }

        public Task DeleteAsync(Session session)
        {
            throw new NotImplementedException();
        }
    }
}
