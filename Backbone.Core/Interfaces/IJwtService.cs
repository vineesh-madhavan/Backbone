//Backbone.Core/Interfaces/IJwtService.cs

using System.Security.Claims;

namespace Backbone.Core.Interfaces;

public interface IJwtService
{
    string GenerateToken(string username, IEnumerable<string> roles, IEnumerable<Claim> additionalClaims = null);
}
