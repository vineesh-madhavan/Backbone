// Infrastructure/Extensions/EntityEntryExtensions.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Backbone.Infrastructure.Extensions
{
    public static class EntityEntryExtensions
    {
        public static bool HasChangedOwnedEntities(this EntityEntry entry, ILogger logger = null)
        {
            using var _ = logger?.BeginScope(new
            {
                EntityType = entry.Metadata.ClrType.Name,
                EntityState = entry.State.ToString(),
                Method = nameof(HasChangedOwnedEntities)
            });

            try
            {
                logger?.LogDebug("Checking for changed owned entities");

                if (entry == null)
                {
                    logger?.LogError("EntityEntry parameter is null");
                    throw new ArgumentNullException(nameof(entry));
                }

                var hasChanges = entry.References.Any(r =>
                {
                    try
                    {
                        return r.TargetEntry != null &&
                               r.TargetEntry.Metadata.IsOwned() &&
                               (r.TargetEntry.State == EntityState.Added ||
                                r.TargetEntry.State == EntityState.Modified);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Error evaluating reference {ReferenceName}", r.Metadata.Name);
                        throw;
                    }
                });

                logger?.LogDebug("Entity {EntityType} has {Status} owned entities changes",
                    entry.Metadata.ClrType.Name,
                    hasChanges ? "some" : "no");

                return hasChanges;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error checking for changed owned entities");
                throw;
            }
        }
    }
}