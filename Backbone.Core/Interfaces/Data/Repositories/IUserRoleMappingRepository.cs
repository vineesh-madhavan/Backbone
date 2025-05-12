using Backbone.Core.Entities;

namespace Backbone.Core.Interfaces.Data.Repositories
{
    public interface IUserRoleMappingRepository : IRepository<UserRoleMapping>
    {
        Task<IReadOnlyList<UserRoleMapping>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserRoleMapping>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
        Task<bool> UserHasRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default);
        Task<bool> UserHasRoleAsync(int userId, string roleName, CancellationToken cancellationToken = default);
    }
}

