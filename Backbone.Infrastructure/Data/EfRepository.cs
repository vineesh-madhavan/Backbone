// Backbone.Infrastructure/Data/EfRepository.cs
using Backbone.Core.Entities;
using Backbone.Core.Interfaces;
using Backbone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _entities
                .AsNoTracking() // Good for read operations
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _entities
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _entities.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            // Just mark as deleted - interceptor will handle the rest
            _entities.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _entities
                .AnyAsync(e => e.Id == id, cancellationToken);
        }

        public async Task<T?> GetFirstOrDefaultAsync(
            ISpecification<T> spec,
            CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(spec)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<T>> ListAsync(
            ISpecification<T> spec,
            CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(spec)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountAsync(
            ISpecification<T> spec,
            CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(spec, true)
                .CountAsync(cancellationToken);
        }

        private IQueryable<T> ApplySpecification(ISpecification<T> spec, bool forCount = false)
        {
            var query = _entities.AsQueryable();

            // Auto-filter soft deleted entities
            if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                query = query.Where(e => !((ISoftDelete)e).IsDeleted);
            }

            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            // Includes
            query = spec.Includes.Aggregate(query,
                (current, include) => current.Include(include));

            query = spec.IncludeStrings.Aggregate(query,
                (current, include) => current.Include(include));

            if (!forCount)
            {
                ApplyOrdering(ref query, spec);
                ApplyPagination(ref query, spec);
            }

            return query;
        }

        private void ApplyOrdering(ref IQueryable<T> query, ISpecification<T> spec)
        {
            if (spec.OrderBy != null)
            {
                query = query.OrderBy(spec.OrderBy);
            }
            else if (spec.OrderByDescending != null)
            {
                query = query.OrderByDescending(spec.OrderByDescending);
            }
            else if (spec.OrderBys?.Count > 0)
            {
                var orderedQuery = (IOrderedQueryable<T>?)null;

                foreach (var orderBy in spec.OrderBys)
                {
                    orderedQuery = orderedQuery == null
                        ? query.OrderBy(orderBy)
                        : orderedQuery.ThenBy(orderBy);
                }

                if (orderedQuery != null)
                    query = orderedQuery;
            }
        }

        private void ApplyPagination(ref IQueryable<T> query, ISpecification<T> spec)
        {
            if (spec.IsPagingEnabled)
            {
                query = query.Skip(spec.Skip).Take(spec.Take);
            }
        }
    }
}