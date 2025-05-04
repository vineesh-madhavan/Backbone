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

        public string? Username => _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(ClaimTypes.Name);

        public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User?
            .FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?
            .Identity?.IsAuthenticated ?? false;
    }
}