// Backbone.Core/Interfaces/ICurrentUserService.cs

using System.Collections.Generic;
using System.Security.Claims;
namespace Backbone.Core.Interfaces
{
    public interface ICurrentUserService
    {
        string UserId { get; }
        string Username { get; }
        bool IsAuthenticated { get; }
        bool IsImpersonating { get; set; }
        string OriginalUsername { get; set; }
        string ImpersonatedRole { get; set; }
        string CurrentRole { get; }
        IEnumerable<string> AvailableRoles { get; }
        IEnumerable<string> Roles { get; }

        bool CanImpersonate();
        IEnumerable<string> GetImpersonatableRoles();
        bool IsInRole(string role);
        bool IsInAnyRole(params string[] roles);
        bool IsInAllRoles(params string[] roles);

        bool IsAdmin();
        bool IsMaster();
        bool IsSubscriber();
        bool IsMasterOrAdmin();

        Claim FindClaim(string claimType);
        IEnumerable<Claim> FindClaims(string claimType);

        IEnumerable<Claim> GetAllClaims();
        bool HasClaim(string claimType, string claimValue);
    }
}