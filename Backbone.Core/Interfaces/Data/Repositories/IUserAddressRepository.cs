//Backbone.Core.Interfaces.Data.Repositories/IUserAddressRepository.cs
using Backbone.Core.Entities;

namespace Backbone.Core.Interfaces.Data.Repositories
{
    public interface IUserAddressRepository : IRepository<UserAddress>
    {
        Task<IReadOnlyList<UserAddress>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    }
}
