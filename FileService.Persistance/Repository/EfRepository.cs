

using FileService.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FileService.Persistance.Repository
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _dbContext;
        private readonly DbSet<T> _dbSet;

        public EfRepository(AppDbContext db)
        {
            _dbContext = db;
            _dbSet = db.Set<T>();
        }

        public async Task<T> AddAsync(T entity, CancellationToken ct = default)
        {
            await _dbSet.AddAsync(entity, ct);
            return entity;
        }

        public void Update(T entity) => _dbSet.Update(entity);

        public void Remove(T entity) => _dbSet.Remove(entity);

        public async Task<T?> GetByIdAsync(object id, CancellationToken ct = default)
            => await _dbSet.FindAsync(new[] { id }, ct);

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => await _dbSet.AnyAsync(predicate, ct);
    }
}
