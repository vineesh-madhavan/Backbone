//Backbone.Core/Entities/UserRole.cs
namespace Backbone.Core.Entities
{
    public class UserRole : BaseEntity
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }

        // Navigation
        public ICollection<UserRoleMapping> UserRoleMappings { get; set; }
    }
}
