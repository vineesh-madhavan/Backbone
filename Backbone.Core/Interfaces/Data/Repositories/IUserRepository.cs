//Backbone.Core.Interfaces.Data.Repositories/IUserRepository.cs
using Backbone.Core.Entities;

namespace Backbone.Core.Interfaces.Data.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetUserWithDetailsAsync(int userId, CancellationToken cancellationToken = default);
        Task<User?> GetUserWithRolesAsync(int userId, CancellationToken cancellationToken = default);
        Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameAsync(string username, bool includeDetails = false, CancellationToken cancellationToken = default);
        Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default);
        Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);

        //Task AddAsync(User user, CancellationToken cancellationToken = default);
        //Task UpdateAsync(User user, CancellationToken cancellationToken = default);
        //Task DeleteAsync(User user, CancellationToken cancellationToken = default);
    }
}
