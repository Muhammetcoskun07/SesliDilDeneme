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
            // Burada "Conversation" için çalışacak, diğer T’ler için de aynı mantık:
            return await _dbSet
                .FirstOrDefaultAsync(e => EF.Property<TId>(e, "ConversationId")!.Equals(id));
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
    }
}
