//Backbone.Core/Entities/UserStatus.cs
namespace Backbone.Core.Entities
{
    public class UserStatus : BaseEntity
    {
        public int UserStatusId { get; set; }
        public string StatusName { get; set; }

        // Navigation
        public ICollection<User> Users { get; set; }
    }
}
