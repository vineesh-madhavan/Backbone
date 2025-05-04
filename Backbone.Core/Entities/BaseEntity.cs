
// Core/Entities/BaseEntity.cs
using Backbone.Core.Interfaces;

namespace Backbone.Core.Entities
{
    public abstract class BaseEntity :
        IAuditableEntity,
        ISoftDelete,
        IHasDomainEvents
    {
        public int Id { get; set; }

        // IAuditableEntity
        public DateTimeOffset CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        // ISoftDelete
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // IHasDomainEvents
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent eventItem) => _domainEvents.Add(eventItem);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}