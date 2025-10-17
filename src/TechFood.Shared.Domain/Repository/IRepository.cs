using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TechFood.Shared.Domain.Repository;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);

    Task<IEnumerable<T>> GetAllAsync();

    Task<Guid> AddAsync(T entity);

    Task DeleteAsync(T entity);
}
