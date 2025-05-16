//Backbone Project/Infrastructure/Data/UnitofWork.cs
using Backbone.Core.Entities;
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using Backbone.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics;

namespace Backbone.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private Hashtable _repositories;

        public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("UnitOfWork initialized");
        }

        public IRepository<T> Repository<T>() where T : BaseEntity
        {
            using var _ = _logger.BeginScope(new { EntityType = typeof(T).Name, Method = nameof(Repository) });

            try
            {
                _logger.LogDebug("Requesting repository for type {EntityType}", typeof(T).Name);

                if (_repositories == null)
                {
                    _logger.LogDebug("Initializing repositories hashtable");
                    _repositories = new Hashtable();
                }

                var type = typeof(T).Name;

                if (!_repositories.ContainsKey(type))
                {
                    _logger.LogDebug("Creating new repository instance for {EntityType}", type);
                    var repositoryType = typeof(EfRepository<>);
                    var repositoryInstance = Activator.CreateInstance(
                        repositoryType.MakeGenericType(typeof(T)), _context, _logger);

                    _repositories.Add(type, repositoryInstance);
                    _logger.LogInformation("Created new repository for {EntityType}", type);
                }
                else
                {
                    _logger.LogDebug("Using existing repository for {EntityType}", type);
                }

                return (IRepository<T>)_repositories[type];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating repository for type {EntityType}", typeof(T).Name);
                throw new RepositoryException($"Could not create repository for type {typeof(T).Name}", ex);
            }
        }

        public async Task<int> CompleteAsync()
        {
            using var _ = _logger.BeginScope(new { Method = nameof(CompleteAsync) });

            try
            {
                _logger.LogDebug("Starting to save changes to database");
                var stopwatch = Stopwatch.StartNew();

                var affectedRows = await _context.SaveChangesAsync();

                stopwatch.Stop();
                _logger.LogInformation("Successfully saved {AffectedRows} changes to database in {ElapsedMilliseconds}ms",
                    affectedRows, stopwatch.ElapsedMilliseconds);

                return affectedRows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw new UnitOfWorkException("Could not save changes to database", ex);
            }
        }

        public void Dispose()
        {
            using var _ = _logger.BeginScope(new { Method = nameof(Dispose) });

            try
            {
                _logger.LogDebug("Disposing UnitOfWork");
                _context.Dispose();
                _logger.LogInformation("Successfully disposed UnitOfWork");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing UnitOfWork");
                throw;
            }
        }
    }

    public class UnitOfWorkException : Exception
    {
        public UnitOfWorkException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class RepositoryException : Exception
    {
        public RepositoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}