// Backbone.Infrastructure.Tests/Mocks/MockCurrentUserService.cs
using Backbone.Core.Interfaces;

namespace Backbone.Infrastructure.Tests.Mocks
{
    public class MockCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; set; } = "test-user-id";
        public string? Username { get; set; } = "testuser";
        public string? Email { get; set; } = "test@example.com";
        public bool IsAuthenticated { get; set; } = true;
        public IEnumerable<string> Roles { get; set; } = new List<string> { "User" };
    }
}