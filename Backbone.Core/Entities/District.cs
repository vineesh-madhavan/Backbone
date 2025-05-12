//Backbone.Core/Entities/District.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backbone.Core.Entities
{
    public class District : BaseEntity
    {
        public int DistrictId { get; set; }
        public int StateId { get; set; }
        public string DistrictName { get; set; }

        // Navigation
        public State State { get; set; }
        public ICollection<UserAddress> UserAddresses { get; set; }
    }
}
