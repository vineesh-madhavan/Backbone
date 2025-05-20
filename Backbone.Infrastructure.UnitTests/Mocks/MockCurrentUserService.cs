// Backbone.Infrastructure.Tests/Mocks/MockCurrentUserService.cs
using Backbone.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Backbone.Infrastructure.Tests.Mocks
{
    public class MockCurrentUserService : ICurrentUserService
    {
        public string? Username { get; set; } = "testuser";
        public string? OriginalUsername { get; set; }
        public bool IsAuthenticated { get; set; } = true;
        public bool IsImpersonating { get; set; }
        public string? ImpersonatedRole { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string> { "Subscriber" };

        public bool IsInRole(string role) => Roles.Contains(role);
        public bool IsInAnyRole(params string[] roles) => Roles.Any(r => roles.Contains(r));
        public bool IsInAllRoles(params string[] roles) => roles.All(r => Roles.Contains(r));
        public bool IsAdmin() => IsInRole("Admin");
        public bool IsMaster() => IsInRole("Master");
        public bool IsSubscriber() => IsInRole("Subscriber");
        public bool IsMasterOrAdmin() => IsInAnyRole("Admin", "Master");
        public bool CanImpersonate() => IsAdmin();
        public IEnumerable<string> GetImpersonatableRoles() => IsAdmin() ? new[] { "Master", "Subscriber" } : Enumerable.Empty<string>();
    }
}