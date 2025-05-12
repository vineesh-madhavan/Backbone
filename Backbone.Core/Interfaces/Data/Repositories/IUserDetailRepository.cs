//Backbone.Core.Interfaces.Data.Repositories/IUserDetailRepository.cs
using Backbone.Core.Entities;

namespace Backbone.Core.Interfaces.Data.Repositories
{
    public interface IUserDetailRepository : IRepository<UserDetail>
    {
        Task<UserDetail?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    }
}
