using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Interfaces;
using SesliDil.Core.Mappings;
using SesliDil.Core.Entities;
using AutoMapper;

namespace SesliDil.Service.Services
{
    public class Service<T> : IService<T> where T : class
    {
        private readonly IRepository<T> _repository;
        private IMapper _mapper;

        public Service(IRepository<T> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper=mapper;
        }

        public virtual async Task<T> GetByIdAsync<TId>(TId id)
        {
            if (id == null) throw new ArgumentNullException("Invalid id");
            return await _repository.GetByIdAsync<TId>(id);
        }
        public virtual async Task CreateAsync(T entity)
        {
            if(entity == null) throw new ArgumentNullException(nameof(entity));
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();
        }
        public virtual async Task UpdateAsync(T entity)
        {
            if(entity == null) throw new ArgumentNullException(nameof(entity));
            _repository.Update(entity);
            await _repository.SaveChangesAsync();
        }
        public virtual async Task DeleteAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _repository.Delete(entity);
            await _repository.SaveChangesAsync();
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }
    }
}
