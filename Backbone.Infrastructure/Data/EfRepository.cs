// Backbone.Infrastructure/Data/EfRepository.cs
//using Backbone.Core.Entities;
//using Backbone.Core.Interfaces;
//using Backbone.Core.Interfaces.Data.Repositories;
//using Backbone.Infrastructure.Persistence;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using System.Linq.Expressions;

//namespace Backbone.Infrastructure.Data
//{
//    public class EfRepository<T> : IRepository<T> where T : BaseEntity
//    {
//        private readonly ApplicationDbContext _dbContext;
//        private readonly DbSet<T> _entities;
//        private readonly ILogger<EfRepository<T>> _logger;

//        public EfRepository(ApplicationDbContext dbContext, ILogger<EfRepository> logger)
//        {
//            _dbContext = dbContext;
//            _entities = dbContext.Set<T>();
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
//        {

//                return await _entities
//                    .AsNoTracking() // Good for read operations
//                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

//        }

//        public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
//        {
//            return await _entities
//                .AsNoTracking()
//                .ToListAsync(cancellationToken);
//        }

//        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
//        {
//            await _entities.AddAsync(entity, cancellationToken);
//            return entity;
//        }

//        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
//        {
//            _dbContext.Entry(entity).State = EntityState.Modified;
//            await Task.CompletedTask;
//        }

//        public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
//        {
//            // Just mark as deleted - interceptor will handle the rest
//            _entities.Remove(entity);
//            await Task.CompletedTask;
//        }

//        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
//        {
//            return await _entities
//                .AnyAsync(e => e.Id == id, cancellationToken);
//        }

//        public async Task<T?> GetFirstOrDefaultAsync(
//            ISpecification<T> spec,
//            CancellationToken cancellationToken = default)
//        {
//            return await ApplySpecification(spec)
//                .AsNoTracking()
//                .FirstOrDefaultAsync(cancellationToken);
//        }

//        public async Task<IReadOnlyList<T>> ListAsync(
//            ISpecification<T> spec,
//            CancellationToken cancellationToken = default)
//        {
//            return await ApplySpecification(spec)
//                .AsNoTracking()
//                .ToListAsync(cancellationToken);
//        }

//        public async Task<int> CountAsync(
//            ISpecification<T> spec,
//            CancellationToken cancellationToken = default)
//        {
//            return await ApplySpecification(spec, true)
//                .CountAsync(cancellationToken);
//        }

//        private IQueryable<T> ApplySpecification(ISpecification<T> spec, bool forCount = false)
//        {
//            var query = _entities.AsQueryable();

//            // Auto-filter soft deleted entities
//            if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
//            {
//                query = query.Where(e => !((ISoftDelete)e).IsDeleted);
//            }

//            if (spec.Criteria != null)
//            {
//                query = query.Where(spec.Criteria);
//            }

//            // Includes
//            query = spec.Includes.Aggregate(query,
//                (current, include) => current.Include(include));

//            query = spec.IncludeStrings.Aggregate(query,
//                (current, include) => current.Include(include));

//            if (!forCount)
//            {
//                ApplyOrdering(ref query, spec);
//                ApplyPagination(ref query, spec);
//            }

//            return query;
//        }

//        private void ApplyOrdering(ref IQueryable<T> query, ISpecification<T> spec)
//        {
//            if (spec.OrderBy != null)
//            {
//                query = query.OrderBy(spec.OrderBy);
//            }
//            else if (spec.OrderByDescending != null)
//            {
//                query = query.OrderByDescending(spec.OrderByDescending);
//            }
//            else if (spec.OrderBys?.Count > 0)
//            {
//                IOrderedQueryable<T>? orderedQuery = null;

//                foreach (var orderBy in spec.OrderBys)
//                {
//                    orderedQuery = orderedQuery == null
//                        ? query.OrderBy(orderBy)
//                        : orderedQuery.ThenBy(orderBy);
//                }

//                if (orderedQuery != null)
//                    query = orderedQuery;
//            }
//            else if (spec.OrderByDescendings?.Count > 0)
//            {
//                IOrderedQueryable<T>? orderedQuery = null;

//                foreach (var orderByDesc in spec.OrderByDescendings)
//                {
//                    orderedQuery = orderedQuery == null
//                        ? query.OrderByDescending(orderByDesc)
//                        : orderedQuery.ThenByDescending(orderByDesc);
//                }

//                if (orderedQuery != null)
//                    query = orderedQuery;
//            }
//        }

//        private void ApplyPagination(ref IQueryable<T> query, ISpecification<T> spec)
//        {
//            if (spec.IsPagingEnabled)
//            {
//                query = query.Skip(spec.Skip).Take(spec.Take);
//            }
//        }
//    }
//}

using Backbone.Core.Entities;
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using Backbone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Backbone.Infrastructure.Data
{
    public class EfRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DbSet<T> _entities;
        private readonly ILogger<EfRepository<T>> _logger;

        public EfRepository(ApplicationDbContext dbContext, ILogger<EfRepository<T>> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _entities = dbContext.Set<T>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("Initialized repository for {EntityType}", typeof(T).Name);
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting {EntityType} with ID {EntityId}", typeof(T).Name, id);

            try
            {
                var entity = await _entities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning("{EntityType} with ID {EntityId} not found", typeof(T).Name, id);
                }
                else
                {
                    _logger.LogDebug("Successfully retrieved {EntityType} with ID {EntityId}",
                        typeof(T).Name, id);
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {EntityType} with ID {EntityId}",
                    typeof(T).Name, id);
                throw;
            }
        }

        public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting all {EntityType} records", typeof(T).Name);

            try
            {
                var entities = await _entities
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} {EntityType} records",
                    entities.Count, typeof(T).Name);

                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all {EntityType} records", typeof(T).Name);
                throw;
            }
        }

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Adding new {EntityType}", typeof(T).Name);

            try
            {
                await _entities.AddAsync(entity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added {EntityType} with ID {EntityId}",
                    typeof(T).Name, entity.Id);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Updating {EntityType} with ID {EntityId}",
                typeof(T).Name, entity.Id);

            try
            {
                _dbContext.Entry(entity).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated {EntityType} with ID {EntityId}",
                    typeof(T).Name, entity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {EntityType} with ID {EntityId}",
                    typeof(T).Name, entity.Id);
                throw;
            }
        }

        public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Deleting {EntityType} with ID {EntityId}",
                typeof(T).Name, entity.Id);

            try
            {
                _entities.Remove(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted {EntityType} with ID {EntityId}",
                    typeof(T).Name, entity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {EntityType} with ID {EntityId}",
                    typeof(T).Name, entity.Id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking existence of {EntityType} with ID {EntityId}",
                typeof(T).Name, id);

            try
            {
                var exists = await _entities.AnyAsync(e => e.Id == id, cancellationToken);
                _logger.LogDebug("{EntityType} with ID {EntityId} exists: {Exists}",
                    typeof(T).Name, id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of {EntityType} with ID {EntityId}",
                    typeof(T).Name, id);
                throw;
            }
        }

        public async Task<T?> GetFirstOrDefaultAsync(
            ISpecification<T> spec,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting first {EntityType} matching specification", typeof(T).Name);

            try
            {
                var entity = await ApplySpecification(spec)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);

                if (entity == null)
                {
                    _logger.LogDebug("No {EntityType} found matching specification", typeof(T).Name);
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting first {EntityType} matching specification",
                    typeof(T).Name);
                throw;
            }
        }

        public async Task<IReadOnlyList<T>> ListAsync(
            ISpecification<T> spec,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listing {EntityType} records matching specification", typeof(T).Name);

            try
            {
                var entities = await ApplySpecification(spec)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Found {Count} {EntityType} records matching specification",
                    entities.Count, typeof(T).Name);

                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing {EntityType} records matching specification",
                    typeof(T).Name);
                throw;
            }
        }

        public async Task<int> CountAsync(
            ISpecification<T> spec,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Counting {EntityType} records matching specification", typeof(T).Name);

            try
            {
                var count = await ApplySpecification(spec, true)
                    .CountAsync(cancellationToken);

                _logger.LogDebug("Found {Count} {EntityType} records matching specification",
                    count, typeof(T).Name);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting {EntityType} records matching specification",
                    typeof(T).Name);
                throw;
            }
        }

        private IQueryable<T> ApplySpecification(ISpecification<T> spec, bool forCount = false)
        {
            var query = _entities.AsQueryable();

            // Auto-filter soft deleted entities
            if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                query = query.Where(e => !((ISoftDelete)e).IsDeleted);
                _logger.LogTrace("Applied soft delete filter for {EntityType}", typeof(T).Name);
            }

            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
                _logger.LogTrace("Applied criteria for {EntityType}", typeof(T).Name);
            }

            // Includes
            query = spec.Includes.Aggregate(query,
                (current, include) =>
                {
                    _logger.LogTrace("Including navigation property {Include} for {EntityType}",
                        include.ToString(), typeof(T).Name);
                    return current.Include(include);
                });

            query = spec.IncludeStrings.Aggregate(query,
                (current, include) =>
                {
                    _logger.LogTrace("Including navigation property {Include} for {EntityType}",
                        include, typeof(T).Name);
                    return current.Include(include);
                });

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
                _logger.LogTrace("Applied OrderBy for {EntityType}", typeof(T).Name);
            }
            else if (spec.OrderByDescending != null)
            {
                query = query.OrderByDescending(spec.OrderByDescending);
                _logger.LogTrace("Applied OrderByDescending for {EntityType}", typeof(T).Name);
            }
            else if (spec.OrderBys?.Count > 0)
            {
                IOrderedQueryable<T>? orderedQuery = null;

                foreach (var orderBy in spec.OrderBys)
                {
                    orderedQuery = orderedQuery == null
                        ? query.OrderBy(orderBy)
                        : orderedQuery.ThenBy(orderBy);
                    _logger.LogTrace("Applied OrderBy for {EntityType}", typeof(T).Name);
                }

                if (orderedQuery != null)
                    query = orderedQuery;
            }
            else if (spec.OrderByDescendings?.Count > 0)
            {
                IOrderedQueryable<T>? orderedQuery = null;

                foreach (var orderByDesc in spec.OrderByDescendings)
                {
                    orderedQuery = orderedQuery == null
                        ? query.OrderByDescending(orderByDesc)
                        : orderedQuery.ThenByDescending(orderByDesc);
                    _logger.LogTrace("Applied OrderByDescending for {EntityType}", typeof(T).Name);
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
                _logger.LogTrace("Applied pagination (Skip: {Skip}, Take: {Take}) for {EntityType}",
                    spec.Skip, spec.Take, typeof(T).Name);
            }
        }
    }
}