// Infrastructure/Interceptors/MasterSaveChangesInterceptor.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Backbone.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using System.Linq;
using System.Collections.Generic;
using Backbone.Infrastructure.Extensions;

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
            var userId = _currentUser.UserId ?? "SYSTEM";

            foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.CreatedBy = userId;
                }

                if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
                {
                    entry.Entity.LastModifiedAt = utcNow;
                    entry.Entity.LastModifiedBy = userId;
                }
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
                    entry.Entity.DeletedBy = _currentUser.UserId;
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