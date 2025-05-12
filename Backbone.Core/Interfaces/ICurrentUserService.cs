// Backbone.Core/Interfaces/ICurrentUserService.cs
namespace Backbone.Core.Interfaces
{
    public interface ICurrentUserService
    {
        string? Username { get; }
        IEnumerable<string> Roles { get; }
        bool IsAuthenticated { get; }

        bool IsInRole(string role);
        bool IsInAnyRole(params string[] roles);
        bool IsInAllRoles(params string[] roles);
        bool IsAdmin();
        bool IsMasterOrAdmin();
    }
}