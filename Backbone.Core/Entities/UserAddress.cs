//Backbone.Core/Entities/UserAddress.cs
using Backbone.Core.Interfaces;

namespace Backbone.Core.Entities
{
    public class UserAddress : BaseEntity, IAuditableEntity, ISoftDelete
    {
        public int UserAddressId { get; set; }
        public int UserId { get; set; }

        // Address info
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public int DistrictId { get; set; }
        public int StateId { get; set; }
        public string PIN { get; set; }

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
        public District District { get; set; }
        public State State { get; set; }
    }
}
