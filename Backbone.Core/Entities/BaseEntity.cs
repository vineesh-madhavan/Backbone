
// Core/Entities/BaseEntity.cs
using Backbone.Core.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Backbone.Core.Entities
{
    public abstract class BaseEntity : IAuditableEntity, ISoftDelete
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        // IAuditableEntity implementation
        public DateTimeOffset CreatedAt { get; set; }
        public string CreatedBy { get; set; } = "system";
        public DateTimeOffset? LastModifiedAt { get; set; }  // Changed from UpdatedAt to match interface
        public string? LastModifiedBy { get; set; }         // Changed from UpdatedBy to match interface

        // ISoftDelete implementation (must have public setters)
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Protected methods for controlled modification
        protected void SetCreated(string createdBy)
        {
            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("CreatedBy cannot be null or empty", nameof(createdBy));

            CreatedAt = DateTimeOffset.UtcNow;
            CreatedBy = createdBy;
            LastModifiedAt = CreatedAt;
            LastModifiedBy = CreatedBy;
        }

        protected void SetLastModified(string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(modifiedBy))
                throw new ArgumentException("ModifiedBy cannot be null or empty", nameof(modifiedBy));

            LastModifiedAt = DateTimeOffset.UtcNow;
            LastModifiedBy = modifiedBy;
        }

        // Public API methods
        public void TrackCreation(string createdBy) => SetCreated(createdBy);
        public void TrackModification(string modifiedBy) => SetLastModified(modifiedBy);

        public virtual void SoftDelete(string deletedBy)
        {
            if (string.IsNullOrWhiteSpace(deletedBy))
                throw new ArgumentException("DeletedBy cannot be null or empty", nameof(deletedBy));

            IsDeleted = true;
            DeletedAt = DateTimeOffset.UtcNow;
            DeletedBy = deletedBy;
            SetLastModified(deletedBy);
        }

        public virtual void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
            SetLastModified("system-restore");
        }

        // Equality implementation...
    }
}