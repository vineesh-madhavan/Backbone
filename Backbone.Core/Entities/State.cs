//Backbone.Core/Entities/District.cs
namespace Backbone.Core.Entities
{
    public class State : BaseEntity
    {
        public int StateId { get; set; }
        public string StateName { get; set; }

        // Navigation
        public ICollection<District> Districts { get; set; }
        public ICollection<UserAddress> UserAddresses { get; set; }
    }
}
