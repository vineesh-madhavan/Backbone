// Backbone.Infrastructure.Tests/Mocks/MockCurrentUserService.cs
using Backbone.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Backbone.Infrastructure.UnitTests.Mocks
{
    public class MockCurrentUserService : ICurrentUserService
    {
        // Mock properties with default values
        public string UserId { get; set; } = "mock-user-id";
        public string Username { get; set; } = "mockuser@test.com";
        public string Email { get; set; } = "mockuser@test.com";
        public bool IsAuthenticated { get; set; } = true;
        public bool IsImpersonating { get; set; }
        public string OriginalUsername { get; set; }
        public string ImpersonatedRole { get; set; }
        public string CurrentRole { get; set; } = "User";
        public IEnumerable<string> AvailableRoles { get; set; } = new List<string> { "User" };

        public List<Claim> Claims { get; set; } = new List<Claim>();

        public MockCurrentUserService()
        {
            // Initialize with default claims
            Claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, UserId),
                new Claim(ClaimTypes.Name, Username),
                new Claim(ClaimTypes.Email, Email),
                new Claim(ClaimTypes.Role, CurrentRole),
                new Claim("original_roles", string.Join(",", AvailableRoles))
            };
        }

        // Interface implementations
        public IEnumerable<string> Roles => Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        public Claim FindClaim(string claimType)
        {
            return Claims.FirstOrDefault(c => c.Type == claimType);
        }

        public IEnumerable<Claim> FindClaims(string claimType)
        {
            return Claims.Where(c => c.Type == claimType);
        }

        public IEnumerable<Claim> GetAllClaims()
        {
            return Claims.AsReadOnly();
        }

        public bool HasClaim(string claimType, string claimValue)
        {
            return Claims.Any(c => c.Type == claimType && c.Value == claimValue);
        }

        public bool IsInRole(string role)
        {
            return Roles.Contains(role);
        }

        public bool IsInAnyRole(params string[] roles)
        {
            return roles.Any(IsInRole);
        }

        public bool IsInAllRoles(params string[] roles)
        {
            return roles.All(IsInRole);
        }

        // Role-specific methods
        public bool IsAdmin() => IsInRole("Admin");
        public bool IsMaster() => IsInRole("Master");
        public bool IsSubscriber() => IsInRole("Subscriber");
        public bool IsMasterOrAdmin() => IsInAnyRole("Admin", "Master");

        // Impersonation methods
        public bool CanImpersonate() => IsAdmin();

        public IEnumerable<string> GetImpersonatableRoles()
        {
            return IsAdmin()
                ? new List<string> { "Master", "Subscriber" }
                : Enumerable.Empty<string>();
        }

        // Test helper methods
        public MockCurrentUserService WithUserId(string userId)
        {
            UserId = userId;
            UpdateClaim(ClaimTypes.NameIdentifier, userId);
            return this;
        }

        public MockCurrentUserService WithUsername(string username)
        {
            Username = username;
            UpdateClaim(ClaimTypes.Name, username);
            return this;
        }

        public MockCurrentUserService WithRoles(params string[] roles)
        {
            AvailableRoles = roles.ToList();
            CurrentRole = roles.FirstOrDefault();

            Claims.RemoveAll(c => c.Type == ClaimTypes.Role);
            Claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            UpdateClaim("original_roles", string.Join(",", roles));

            return this;
        }

        public MockCurrentUserService WithClaim(string type, string value)
        {
            Claims.Add(new Claim(type, value));
            return this;
        }

        private void UpdateClaim(string claimType, string value)
        {
            var existing = Claims.FirstOrDefault(c => c.Type == claimType);
            if (existing != null)
            {
                Claims.Remove(existing);
            }
            Claims.Add(new Claim(claimType, value));
        }
    }
}