// Infrastructure/Extensions/EntityEntryExtensions.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Backbone.Infrastructure.Extensions
{
    public static class EntityEntryExtensions
    {
        public static bool HasChangedOwnedEntities(this EntityEntry entry)
        {
            return entry.References.Any(r =>
                r.TargetEntry != null &&
                r.TargetEntry.Metadata.IsOwned() &&
                (r.TargetEntry.State == EntityState.Added ||
                 r.TargetEntry.State == EntityState.Modified));
        }
    }
}