//Backbone.Core/Entities/User.cs
using Backbone.Core.Interfaces;

namespace Backbone.Core.Entities
{
    public class User : BaseEntity, IAuditableEntity, ISoftDelete
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public int StatusId { get; set; } // FK to UserStatus

        // Audit fields (from IAuditableEntity)
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string LastModifiedBy { get; set; }

        // Soft delete (from ISoftDelete)
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string DeletedBy { get; set; }

        // Navigation properties
        public UserStatus Status { get; set; }
        public ICollection<UserDetail> UserDetails { get; set; } = new List<UserDetail>();
        public ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
        public ICollection<UserRoleMapping> UserRoleMappings { get; set; } = new List<UserRoleMapping>();
    }
}
