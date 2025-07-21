using SesliDil.Core.Interfaces;
using SesliDil.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SesliDil.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly SesliDilDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(SesliDilDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T> GetByIdAsync<TId>(TId id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

      
    }
}
