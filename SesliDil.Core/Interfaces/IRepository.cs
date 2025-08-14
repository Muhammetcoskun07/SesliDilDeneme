using SesliDil.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync<TId>(TId id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<int> SaveChangesAsync(); 
        IQueryable<T> Query();
        Task UpdateAsync(Conversation conversation);
        Task<T> FindAsync(object id);
        Task<Conversation> GetByIdWithMessagesAsync(string id);
        Task UpdateAsync(T entity);
        Task<List<Message>> GetByConversationIdAsync(string conversationId);
    }
}
