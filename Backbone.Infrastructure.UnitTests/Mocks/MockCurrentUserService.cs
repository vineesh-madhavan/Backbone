// Backbone.Infrastructure.Tests/Mocks/MockCurrentUserService.cs
using Backbone.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Backbone.Infrastructure.Tests.Mocks
{
    public class MockCurrentUserService : ICurrentUserService
    {
        public string? Username { get; set; } = "testuser";
        public bool IsAuthenticated { get; set; } = true;
        public IEnumerable<string> Roles { get; set; } = new List<string> { "Subscriber" };

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