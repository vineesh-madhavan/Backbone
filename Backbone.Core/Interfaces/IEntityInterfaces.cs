// Core/Interfaces/IEntityInterfaces.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backbone.Core.Interfaces
{

    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }

    // Auditing
    public interface IAuditableEntity
    {
        DateTimeOffset CreatedAt { get; set; }
        string CreatedBy { get; set; }
        DateTimeOffset? LastModifiedAt { get; set; }
        string? LastModifiedBy { get; set; }
    }

    // Soft Delete
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        DateTimeOffset? DeletedAt { get; set; }
        string? DeletedBy { get; set; }
    }

    // Domain Events
    public interface IHasDomainEvents
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void ClearDomainEvents();
        void AddDomainEvent(IDomainEvent eventItem);
    }

    // Validation
    public interface IValidatableEntity
    {
        bool IsValid(out IEnumerable<string> validationErrors);
    }

    //// Current User Service
    //public interface ICurrentUserService
    //{
    //    string? UserId { get; }
    //    string? Username { get; }
    //}
}