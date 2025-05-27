//Backbone.Infrastructure/Services/JwtService.cs
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Backbone.Core.Settings;
using Backbone.Core.Interfaces;

namespace Backbone.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<JwtService> _logger;
        private readonly int _initialTokenExpirationMinutes;

        public JwtService(
            IOptions<JwtSettings> jwtSettings,
            IOptions<AuthSettings> authSettings,
            ILogger<JwtService> logger)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _initialTokenExpirationMinutes = authSettings.Value.InitialTokenExpirationMinutes;

            ValidateJwtSettings();
            ValidateAuthSettings();
        }

        private void ValidateJwtSettings()
        {
            if (string.IsNullOrWhiteSpace(_jwtSettings.Secret))
                throw new ArgumentNullException(nameof(_jwtSettings.Secret), "JWT Secret is required");

            if (_jwtSettings.Secret.Length < 32)
                throw new ArgumentException("JWT Secret must be at least 256 bits (32 characters)");

            if (string.IsNullOrWhiteSpace(_jwtSettings.Issuer))
                throw new ArgumentNullException(nameof(_jwtSettings.Issuer), "JWT Issuer is required");

            if (string.IsNullOrWhiteSpace(_jwtSettings.Audience))
                throw new ArgumentNullException(nameof(_jwtSettings.Audience), "JWT Audience is required");

            if (_jwtSettings.ExpirationInMinutes <= 0)
                throw new ArgumentException("JWT Expiration must be greater than 0");
        }

        private void ValidateAuthSettings()
        {
            if (_initialTokenExpirationMinutes <= 0)
                throw new ArgumentException("Initial token expiration must be greater than 0");
        }

        public string GenerateToken(string username, IEnumerable<string> roles, IEnumerable<Claim> additionalClaims = null)
        {
            return GenerateToken(username, roles, _jwtSettings.ExpirationInMinutes, additionalClaims);
        }

        public string GenerateToken(string username, IEnumerable<string> roles, int expirationMinutes, IEnumerable<Claim> additionalClaims = null)
        {
            _logger.LogInformation("Generating JWT for {Username} with roles: {Roles}",
                username, string.Join(",", roles));

            try
            {
                ValidateInput(username, roles);

                var claims = BuildClaimsList(username, roles, additionalClaims);
                var tokenDescriptor = CreateTokenDescriptor(claims, expirationMinutes);

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = tokenHandler.WriteToken(token);

                LogTokenGenerationSuccess(username, token);

                return jwtToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate JWT for {Username}", username);
                throw new SecurityTokenException("Token generation failed", ex);
            }
        }

        public string GenerateDirectLoginToken(string username, string role, IEnumerable<string> allRoles)
        {
            if (string.IsNullOrEmpty(role))
                throw new ArgumentException("Role cannot be null or empty", nameof(role));

            if (!allRoles.Contains(role))
                throw new ArgumentException("Specified role is not available to user", nameof(role));

            var claims = new List<Claim>
        {
            new Claim("original_roles", string.Join(",", allRoles)),
            new Claim("current_role", role)
        };

            return GenerateToken(username, new[] { role }, _jwtSettings.ExpirationInMinutes, claims);
        }

        public string GenerateInitialToken(string username, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim("original_roles", string.Join(",", roles))
            };

            return GenerateToken(username, Enumerable.Empty<string>(), _initialTokenExpirationMinutes, claims);
        }

        public string GenerateRoleSpecificToken(string username, string selectedRole, IEnumerable<string> allRoles)
        {
            var claims = new List<Claim>
            {
                new Claim("original_roles", string.Join(",", allRoles)),
                new Claim("current_role", selectedRole)
            };

            return GenerateToken(username, new[] { selectedRole }, _jwtSettings.ExpirationInMinutes, claims);
        }

        private void ValidateInput(string username, IEnumerable<string> roles)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            if (roles == null)
                throw new ArgumentNullException(nameof(roles), "Roles collection cannot be null");
        }

        private List<Claim> BuildClaimsList(string username, IEnumerable<string> roles, IEnumerable<Claim> additionalClaims)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username)
            };

            if (roles.Any())
            {
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            }
            else
            {
                _logger.LogWarning("No roles provided for user {Username}, adding default role", username);
                claims.Add(new Claim(ClaimTypes.Role, "DefaultRole"));
            }

            if (additionalClaims != null)
            {
                claims.AddRange(additionalClaims);
            }

            return claims;
        }

        private SecurityTokenDescriptor CreateTokenDescriptor(IEnumerable<Claim> claims, int expirationMinutes)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            return new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = creds,
                NotBefore = DateTime.UtcNow
            };
        }

        private void LogTokenGenerationSuccess(string username, SecurityToken token)
        {
            _logger.LogDebug("Successfully generated JWT for {Username} with details: " +
                           "TokenId: {TokenId}, " +
                           "Issuer: {Issuer}, " +
                           "Audience: {Audience}, " +
                           "IssuedAt: {IssuedAt}, " +
                           "Expires: {Expires}",
                username,
                token.Id,
                _jwtSettings.Issuer,
                _jwtSettings.Audience,
                token.ValidFrom,
                token.ValidTo);
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                _logger.LogDebug("Successfully validated JWT with ID: {TokenId}", validatedToken.Id);

                return principal;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning(ex, "Expired JWT token");
                throw;
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning(ex, "Invalid JWT signature");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT validation failed");
                throw;
            }
        }

        public IEnumerable<string> GetOriginalRolesFromToken(string token)
        {
            var principal = ValidateToken(token);
            var originalRolesClaim = principal.FindFirst("original_roles")?.Value;
            return originalRolesClaim?.Split(',') ?? Enumerable.Empty<string>();
        }

        public string GetCurrentRoleFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal.FindFirst("current_role")?.Value;
        }
    }
}