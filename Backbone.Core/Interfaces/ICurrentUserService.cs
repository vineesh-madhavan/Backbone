// Backbone.Core/Interfaces/ICurrentUserService.cs
namespace Backbone.Core.Interfaces
{
    public interface ICurrentUserService
    {
        string? Username { get; }
        string? OriginalUsername { get; set; }
        IEnumerable<string> Roles { get; }
        bool IsAuthenticated { get; }
        bool IsImpersonating { get; set; }
        string? ImpersonatedRole { get; set; }

        bool IsInRole(string role);
        bool IsInAnyRole(params string[] roles);
        bool IsInAllRoles(params string[] roles);
        bool IsAdmin();
        bool IsMaster();
        bool IsSubscriber();
        bool IsMasterOrAdmin();

        bool CanImpersonate();
        IEnumerable<string> GetImpersonatableRoles();
    }
}