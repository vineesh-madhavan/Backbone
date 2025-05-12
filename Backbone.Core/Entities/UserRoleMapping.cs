//Backbone.Core/Entities/UserRoleMapping.cs
using Backbone.Core.Interfaces;

namespace Backbone.Core.Entities
{
    public class UserRoleMapping : BaseEntity, IAuditableEntity, ISoftDelete
    {
        public int RoleMapId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }

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
        public UserRole Role { get; set; }
    }
}
