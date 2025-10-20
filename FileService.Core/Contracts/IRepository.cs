using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Core.Contracts
{
    public interface IRepository<T> where T : class
    {
        Task<T> AddAsync(T entity, CancellationToken ct = default);
        void Update(T entity);
        void Remove(T entity);
        Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    }
}
