using Backbone.Core.Entities;
using Backbone.Core.Interfaces;
using Backbone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backbone.Infrastructure.Data
{
    public class EfRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DbSet<T> _entities;

        public EfRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _entities = dbContext.Set<T>();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _entities.FindAsync(id);
        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _entities.ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _entities.AddAsync(entity);
            return entity;
        }

        public Task UpdateAsync(T entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _dbContext.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity)
        {
            _entities.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _entities.AnyAsync(e => e.Id == id);
        }

        // Specification pattern methods

        public async Task<T> GetFirstOrDefaultAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).ToListAsync();
        }

        public async Task<int> CountAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec, true).CountAsync();
        }

        private IQueryable<T> ApplySpecification(ISpecification<T> spec, bool forCount = false)
        {
            var query = _entities.AsQueryable();

            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            // Includes (eager loading)
            query = spec.Includes.Aggregate(query,
                (current, include) => current.Include(include));

            if (!forCount)
            {
                // Ordering
                if (spec.OrderBy != null)
                {
                    query = query.OrderBy(spec.OrderBy);
                }
                else if (spec.OrderByDescending != null)
                {
                    query = query.OrderByDescending(spec.OrderByDescending);
                }

                // Pagination
                if (spec.IsPagingEnabled)
                {
                    query = query.Skip(spec.Skip).Take(spec.Take);
                }
            }

            return query;
        }
    }
}
