// Infrastructure/Interceptors/MasterSaveChangesInterceptor.cs
using Backbone.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Backbone.Infrastructure.Interceptors
{
    public class MasterSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly TimeProvider _timeProvider;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public MasterSaveChangesInterceptor(
            TimeProvider timeProvider,
            IMediator mediator,
            ICurrentUserService currentUser)
        {
            _timeProvider = timeProvider;
            _mediator = mediator;
            _currentUser = currentUser;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
                return await base.SavingChangesAsync(eventData, result, cancellationToken);

            await ProcessDomainEvents(eventData.Context);
            ProcessAuditableEntities(eventData.Context);
            ProcessSoftDeletes(eventData.Context);
            ValidateEntities(eventData.Context);

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ProcessAuditableEntities(DbContext context)
        {
            var utcNow = _timeProvider.GetUtcNow();
            var userName = _currentUser.Username ?? "SYSTEM";

            foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.CreatedAt == default)
                    {
                        entry.Entity.CreatedAt = utcNow;
                    }
                    entry.Entity.CreatedBy = userName;
                }

                // Always set modified fields (even if just owned entities changed)
                entry.Entity.LastModifiedAt = utcNow;
                entry.Entity.LastModifiedBy = userName;
            }
        }

        private void ProcessSoftDeletes(DbContext context)
        {
            foreach (var entry in context.ChangeTracker.Entries<ISoftDelete>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = _timeProvider.GetUtcNow();
                    entry.Entity.DeletedBy = _currentUser.Username;
                }
            }
        }

        private async Task ProcessDomainEvents(DbContext context)
        {
            var domainEventEntities = context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Where(x => x.Entity.DomainEvents.Any())
                .Select(x => x.Entity)
                .ToList();

            var domainEvents = domainEventEntities
                .SelectMany(x => x.DomainEvents)
                .ToList();

            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent);
            }

            domainEventEntities.ForEach(x => x.ClearDomainEvents());
        }

        private void ValidateEntities(DbContext context)
        {
            var errors = new List<string>();

            foreach (var entry in context.ChangeTracker.Entries<IValidatableEntity>())
            {
                if (!entry.Entity.IsValid(out var validationErrors))
                {
                    errors.AddRange(validationErrors);
                }
            }

            if (errors.Any())
                throw new DbUpdateException($"Validation failed: {string.Join(", ", errors)}");
        }
    }
}