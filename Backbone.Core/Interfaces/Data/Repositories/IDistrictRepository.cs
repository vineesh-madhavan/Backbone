using Backbone.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backbone.Core.Interfaces.Data.Repositories
{
    public interface IDistrictRepository : IRepository<District>
    {
        Task<IReadOnlyList<District>> GetByStateIdAsync(int stateId, CancellationToken cancellationToken = default);
        Task<District?> GetByNameAsync(string districtName, CancellationToken cancellationToken = default);
    }
}
