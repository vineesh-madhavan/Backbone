//Backbone.Core/Entities/UserDetail.cs
using Backbone.Core.Interfaces;

namespace Backbone.Core.Entities
{
    public class UserDetail : BaseEntity, IAuditableEntity, ISoftDelete
    {
        public int UserDetailId { get; set; }
        public int UserId { get; set; }

        // Personal info
        public string Salutation { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Gender { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public DateTime? DateOfBirth { get; set; }

        // Government IDs
        public string PANNumber { get; set; }
        public string AadhaarNumber { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string LastModifiedBy { get; set; }

        // Soft delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string DeletedBy { get; set; }

        // Navigation
        public User User { get; set; }
    }
}
