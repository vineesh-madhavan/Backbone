// Backbone.Infrastructure/Services/CurrentUserService.cs

using Backbone.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Backbone.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CurrentUserService> _logger;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CurrentUserService> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogDebug("CurrentUserService initialized for context {TraceIdentifier}",
                _httpContextAccessor.HttpContext?.TraceIdentifier);
        }

        public string? Username
        {
            get
            {
                try
                {
                    var username = _httpContextAccessor.HttpContext?.User?
                        .FindFirstValue(ClaimTypes.Name);

                    _logger.LogTrace("Retrieved username: {Username}", username);
                    return username;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve username from claims");
                    return null;
                }
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                try
                {
                    var isAuthenticated = _httpContextAccessor.HttpContext?.User?
                        .Identity?.IsAuthenticated ?? false;

                    _logger.LogTrace("Authentication status: {IsAuthenticated}", isAuthenticated);
                    return isAuthenticated;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to determine authentication status");
                    return false;
                }
            }
        }

        public IEnumerable<string> Roles
        {
            get
            {
                try
                {
                    var roles = _httpContextAccessor.HttpContext?.User?
                        .FindAll(ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList() ?? new List<string>();

                    _logger.LogDebug("User {Username} has roles: {Roles}", Username, string.Join(", ", roles));
                    return roles;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve user roles");
                    return Enumerable.Empty<string>();
                }
            }
        }

        public bool IsInRole(string role)
        {
            using var _ = _logger.BeginScope(new { RoleCheck = role });

            try
            {
                if (string.IsNullOrWhiteSpace(role))
                {
                    _logger.LogWarning("Role check with empty role parameter");
                    return false;
                }

                var hasRole = Roles.Contains(role);
                _logger.LogTrace("Role check result for {Role}: {HasRole}", role, hasRole);
                return hasRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check role membership for {Role}", role);
                return false;
            }
        }

        public bool IsInAnyRole(params string[] roles)
        {
            using var _ = _logger.BeginScope(new { Roles = roles });

            try
            {
                if (roles == null || roles.Length == 0)
                {
                    _logger.LogWarning("Empty roles list provided for IsInAnyRole check");
                    return false;
                }

                var result = Roles.Any(r => roles.Contains(r));
                _logger.LogTrace("IsInAnyRole check result: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check IsInAnyRole for roles: {Roles}", string.Join(", ", roles));
                return false;
            }
        }

        public bool IsInAllRoles(params string[] roles)
        {
            using var _ = _logger.BeginScope(new { Roles = roles });

            try
            {
                if (roles == null || roles.Length == 0)
                {
                    _logger.LogWarning("Empty roles list provided for IsInAllRoles check");
                    return false;
                }

                var result = roles.All(r => Roles.Contains(r));
                _logger.LogTrace("IsInAllRoles check result: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check IsInAllRoles for roles: {Roles}", string.Join(", ", roles));
                return false;
            }
        }

        public bool IsAdmin()
        {
            try
            {
                var isAdmin = IsInRole("Admin");
                _logger.LogTrace("IsAdmin check result: {IsAdmin}", isAdmin);
                return isAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check Admin role");
                return false;
            }
        }

        public bool IsMasterOrAdmin()
        {
            try
            {
                var result = IsInAnyRole("Admin", "Master");
                _logger.LogTrace("IsMasterOrAdmin check result: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check Master/Admin roles");
                return false;
            }
        }
    }
}