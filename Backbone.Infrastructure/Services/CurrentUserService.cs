//Backbone.Infrastructure/Services/CurrentUserService.cs
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

        // Fields to store impersonation state
        private bool _isImpersonating;
        private string? _originalUsername;
        private string? _impersonatedRole;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CurrentUserService> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string UserId => GetClaimValue(ClaimTypes.NameIdentifier);
        public string Username => GetClaimValue(ClaimTypes.Name);
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public bool IsImpersonating
        {
            get => _isImpersonating || GetClaimValue("is_impersonating") == "true";
            set => _isImpersonating = value;
        }

        public string OriginalUsername
        {
            get => _originalUsername ?? (IsImpersonating ? GetClaimValue("original_username") : null);
            set => _originalUsername = value;
        }

        public string ImpersonatedRole
        {
            get => _impersonatedRole ?? (IsImpersonating ? GetClaimValue("impersonation_role") : null);
            set => _impersonatedRole = value;
        }

        public string CurrentRole => GetClaimValue("current_role") ?? Roles.FirstOrDefault();

        public IEnumerable<string> AvailableRoles
        {
            get
            {
                var originalRoles = GetClaimValue("original_roles");
                return string.IsNullOrEmpty(originalRoles)
                    ? Enumerable.Empty<string>()
                    : originalRoles.Split(',');
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

                    // Filter roles if impersonating with a specific role
                    if (IsImpersonating && !string.IsNullOrEmpty(ImpersonatedRole))
                    {
                        roles = roles.Where(r => r == ImpersonatedRole).ToList();
                    }

                    _logger.LogDebug("User {Username} has roles: {Roles}", GetUserDisplayName(), string.Join(", ", roles));
                    return roles;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve user roles");
                    return Enumerable.Empty<string>();
                }
            }
        }

        public bool CanImpersonate() => IsAdmin();

        public IEnumerable<string> GetImpersonatableRoles()
        {
            // Only Admin can impersonate, and can impersonate as Master or Subscriber
            return IsAdmin() ? new List<string> { "Master", "Subscriber" } : Enumerable.Empty<string>();
        }

        public bool IsInRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                _logger.LogWarning("Role check with empty role parameter");
                return false;
            }

            return Roles.Contains(role);
        }

        public bool IsInAnyRole(params string[] roles)
        {
            if (roles == null || roles.Length == 0)
            {
                _logger.LogWarning("Empty roles list provided for IsInAnyRole check");
                return false;
            }

            return Roles.Any(r => roles.Contains(r));
        }

        public bool IsInAllRoles(params string[] roles)
        {
            if (roles == null || roles.Length == 0)
            {
                _logger.LogWarning("Empty roles list provided for IsInAllRoles check");
                return false;
            }

            return roles.All(r => Roles.Contains(r));
        }

        public bool IsAdmin() => IsInRole("Admin");
        public bool IsMaster() => IsInRole("Master");
        public bool IsSubscriber() => IsInRole("Subscriber");
        public bool IsMasterOrAdmin() => IsInAnyRole("Admin", "Master");

        public Claim FindClaim(string claimType)
        {
            try
            {
                return _httpContextAccessor.HttpContext?.User?.FindFirst(claimType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve claim {ClaimType}", claimType);
                return null;
            }
        }

        public IEnumerable<Claim> FindClaims(string claimType)
        {
            try
            {
                return _httpContextAccessor.HttpContext?.User?.FindAll(claimType) ?? Enumerable.Empty<Claim>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve claims {ClaimType}", claimType);
                return Enumerable.Empty<Claim>();
            }
        }

        private string GetClaimValue(string claimType)
        {
            try
            {
                return _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve claim {ClaimType}", claimType);
                return null;
            }
        }

        private string GetUserDisplayName()
        {
            return IsImpersonating
                ? $"{OriginalUsername} (impersonating {Username})"
                : Username ?? "anonymous";
        }

        public IEnumerable<Claim> GetAllClaims()
        {
            try
            {
                return _httpContextAccessor.HttpContext?.User?.Claims ?? Enumerable.Empty<Claim>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user claims");
                return Enumerable.Empty<Claim>();
            }
        }

        public bool HasClaim(string claimType, string claimValue)
        {
            try
            {
                return _httpContextAccessor.HttpContext?.User?
                    .Claims.Any(c => c.Type == claimType && c.Value == claimValue) ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check claim {ClaimType}={ClaimValue}", claimType, claimValue);
                return false;
            }
        }
    }
}