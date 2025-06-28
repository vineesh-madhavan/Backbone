//Backbone.Core/Interfaces/IJwtService.cs

using System.Security.Claims;
using System.Collections.Generic;

namespace Backbone.Core.Interfaces
{
    public interface IJwtService
    {
        // Standard token generation with default expiration
        string GenerateToken(string username, IEnumerable<string> roles, IEnumerable<Claim> additionalClaims = null);

        // Token generation with custom expiration
        string GenerateToken(string username, IEnumerable<string> roles, int expirationMinutes, IEnumerable<Claim> additionalClaims = null);

        // Generates initial short-lived token for role selection flow
        string GenerateInitialToken(string username, IEnumerable<string> roles);

        // Generates final token with selected role
        string GenerateRoleSpecificToken(string username, string selectedRole, IEnumerable<string> allRoles, IEnumerable<Claim> additionalClaims = null);

        // Validates token and returns ClaimsPrincipal
        ClaimsPrincipal ValidateToken(string token);

        // Gets all available roles from token
        IEnumerable<string> GetOriginalRolesFromToken(string token);

        // Gets currently selected role from token
        string GetCurrentRoleFromToken(string token);

        string GenerateDirectLoginToken(string username, string role, IEnumerable<string> allRoles);

        string GenerateTokenWithClaims(string username, IEnumerable<string> roles, IEnumerable<Claim> additionalClaims);
        string GenerateImpersonationToken(string originalUsername, string impersonatedUsername, string role, IEnumerable<string> allRoles);
        IEnumerable<Claim> FilterClaims(IEnumerable<Claim> claims, params string[] claimTypesToExclude);
    }
}