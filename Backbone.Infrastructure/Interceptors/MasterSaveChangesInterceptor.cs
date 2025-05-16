// Infrastructure/Interceptors/MasterSaveChangesInterceptor.cs
using Backbone.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Backbone.Infrastructure.Interceptors
{
    public class MasterSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly TimeProvider _timeProvider;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<MasterSaveChangesInterceptor> _logger;

        public MasterSaveChangesInterceptor(
            TimeProvider timeProvider,
            IMediator mediator,
            ICurrentUserService currentUser,
            ILogger<MasterSaveChangesInterceptor> logger)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new
            {
                ContextType = eventData.Context?.GetType().Name,
                Method = nameof(SavingChangesAsync)
            });

            try
            {
                _logger.LogDebug("Starting SaveChanges interception");

                if (eventData.Context is null)
                {
                    _logger.LogWarning("DbContext is null in SaveChanges interception");
                    return await base.SavingChangesAsync(eventData, result, cancellationToken);
                }

                var stopwatch = Stopwatch.StartNew();

                await ProcessDomainEvents(eventData.Context, cancellationToken);
                ProcessAuditableEntities(eventData.Context);
                ProcessSoftDeletes(eventData.Context);
                ValidateEntities(eventData.Context);

                stopwatch.Stop();
                _logger.LogInformation("Completed SaveChanges interception in {ElapsedMilliseconds}ms",
                    stopwatch.ElapsedMilliseconds);

                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SaveChanges interception");
                throw;
            }
        }

        private void ProcessAuditableEntities(DbContext context)
        {
            try
            {
                _logger.LogDebug("Processing auditable entities");
                var utcNow = _timeProvider.GetUtcNow();
                var userName = _currentUser.Username ?? "SYSTEM";
                var auditableEntries = context.ChangeTracker.Entries<IAuditableEntity>().ToList();

                _logger.LogDebug("Found {Count} auditable entities", auditableEntries.Count);

                foreach (var entry in auditableEntries)
                {
                    try
                    {
                        if (entry.State == EntityState.Added)
                        {
                            if (entry.Entity.CreatedAt == default)
                            {
                                entry.Entity.CreatedAt = utcNow;
                                _logger.LogTrace("Set CreatedAt for {EntityType} to {CreatedAt}",
                                    entry.Metadata.ClrType.Name, utcNow);
                            }
                            entry.Entity.CreatedBy = userName;
                            _logger.LogTrace("Set CreatedBy for {EntityType} to {CreatedBy}",
                                entry.Metadata.ClrType.Name, userName);
                        }

                        entry.Entity.LastModifiedAt = utcNow;
                        entry.Entity.LastModifiedBy = userName;
                        _logger.LogTrace("Updated LastModified fields for {EntityType}",
                            entry.Metadata.ClrType.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing auditable entity {EntityType}",
                            entry.Metadata.ClrType.Name);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessAuditableEntities");
                throw;
            }
        }

        private void ProcessSoftDeletes(DbContext context)
        {
            try
            {
                _logger.LogDebug("Processing soft deletes");
                var softDeleteEntries = context.ChangeTracker.Entries<ISoftDelete>()
                    .Where(e => e.State == EntityState.Deleted)
                    .ToList();

                _logger.LogDebug("Found {Count} entities to soft delete", softDeleteEntries.Count);

                foreach (var entry in softDeleteEntries)
                {
                    try
                    {
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = _timeProvider.GetUtcNow();
                        entry.Entity.DeletedBy = _currentUser.Username;

                        _logger.LogInformation("Soft deleted {EntityType} with ID {EntityId}",
                            entry.Metadata.ClrType.Name,
                            entry.Entity.GetType().GetProperty("Id")?.GetValue(entry.Entity));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing soft delete for {EntityType}",
                            entry.Metadata.ClrType.Name);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessSoftDeletes");
                throw;
            }
        }

        private async Task ProcessDomainEvents(DbContext context, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing domain events");
                var domainEventEntities = context.ChangeTracker
                    .Entries<IHasDomainEvents>()
                    .Where(x => x.Entity.DomainEvents.Any())
                    .Select(x => x.Entity)
                    .ToList();

                var domainEvents = domainEventEntities
                    .SelectMany(x => x.DomainEvents)
                    .ToList();

                _logger.LogDebug("Found {Count} entities with {TotalEvents} domain events",
                    domainEventEntities.Count, domainEvents.Count);

                foreach (var domainEvent in domainEvents)
                {
                    try
                    {
                        _logger.LogInformation("Publishing domain event {EventType}",
                            domainEvent.GetType().Name);
                        await _mediator.Publish(domainEvent, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error publishing domain event {EventType}",
                            domainEvent.GetType().Name);
                        throw;
                    }
                }

                domainEventEntities.ForEach(x => x.ClearDomainEvents());
                _logger.LogDebug("Cleared domain events from entities");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessDomainEvents");
                throw;
            }
        }

        private void ValidateEntities(DbContext context)
        {
            try
            {
                _logger.LogDebug("Validating entities");
                var errors = new List<string>();
                var validatableEntries = context.ChangeTracker.Entries<IValidatableEntity>().ToList();

                _logger.LogDebug("Found {Count} validatable entities", validatableEntries.Count);

                foreach (var entry in validatableEntries)
                {
                    try
                    {
                        if (!entry.Entity.IsValid(out var validationErrors))
                        {
                            errors.AddRange(validationErrors);
                            _logger.LogWarning("Validation failed for {EntityType}: {Errors}",
                                entry.Metadata.ClrType.Name,
                                string.Join(", ", validationErrors));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error validating entity {EntityType}",
                            entry.Metadata.ClrType.Name);
                        throw;
                    }
                }

                if (errors.Any())
                {
                    var errorMessage = $"Validation failed: {string.Join(", ", errors)}";
                    _logger.LogError("Entity validation errors: {Errors}", errorMessage);
                    throw new DbUpdateException(errorMessage);
                }

                _logger.LogDebug("All entities validated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateEntities");
                throw;
            }
        }
    }
}