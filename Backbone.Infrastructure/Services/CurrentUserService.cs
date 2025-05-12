// Backbone.Infrastructure/Services/CurrentUserService.cs

using Backbone.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Linq;

namespace Backbone.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Primary identifier from claims
        public string? Username => _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(ClaimTypes.Name);

        // Note: UserId is not in claims - will need to be fetched from database when needed
        // Removed the UserId property since it's not in the token

        // Removed Email property since it's in UserDetail entity
        // and not included in the JWT claims

        public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User?
            .FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?
            .Identity?.IsAuthenticated ?? false;

        // Role check helper methods
        public bool IsInRole(string role)
        {
            return Roles.Contains(role);
        }

        public bool IsInAnyRole(params string[] roles)
        {
            return Roles.Any(r => roles.Contains(r));
        }

        public bool IsInAllRoles(params string[] roles)
        {
            return roles.All(r => Roles.Contains(r));
        }

        public bool IsAdmin()
        {
            return IsInRole("Admin");
        }

        public bool IsMasterOrAdmin()
        {
            return IsInAnyRole("Admin", "Master");
        }
    }
}