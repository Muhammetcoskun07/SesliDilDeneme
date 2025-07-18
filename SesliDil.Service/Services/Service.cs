using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Interfaces;
using SesliDil.Core.Mappings;

namespace SesliDil.Service.Services
{
    public class Service<T> : IService<T> where T : class
    {
        private readonly IRepository<T> _repository;
        
        public Service(IRepository<T> repository)
        {
            _repository = repository;
            
        }

        public async Task<T> GetByIdAsync(int id)
        {
            if(id==null) throw new ArgumentException("Not Found");
            return await _repository.GetByIdAsync(id);
        }
        public async Task CreateAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
             await _repository.AddAsync(entity);
        }
        public void  Update(T entity)
        {
            if(entity == null) throw new ArgumentNullException(nameof(entity));
            _repository.Update(entity);
        }
        public void  Delete(T entity)
        {
             _repository.Delete(entity);
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }


    }
}
