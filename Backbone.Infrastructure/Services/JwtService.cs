//Backbone.Infrastructure/Services/JwtService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backbone.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Backbone.Core.Settings;

namespace Backbone.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ValidateJwtSettings();
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

        public string GenerateToken(string username, IEnumerable<string> roles, IEnumerable<Claim> additionalClaims = null)
        {
            _logger.LogInformation("Generating JWT for {Username} with roles: {Roles}",
                username, string.Join(",", roles));

            try
            {
                ValidateInput(username, roles);

                var claims = BuildClaimsList(username, roles, additionalClaims);
                var tokenDescriptor = CreateTokenDescriptor(claims);

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
                new Claim(JwtRegisteredClaimNames.Email, username),
                new Claim(ClaimTypes.Name, username)
            };

            // Add roles if any exist, otherwise add default role
            if (roles.Any())
            {
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            }
            else
            {
                _logger.LogWarning("No roles provided for user {Username}, adding default role", username);
                claims.Add(new Claim(ClaimTypes.Role, "DefaultRole"));
            }

            // Add any additional claims
            if (additionalClaims != null)
            {
                claims.AddRange(additionalClaims);
            }

            return claims;
        }

        private SecurityTokenDescriptor CreateTokenDescriptor(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            return new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
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
                    ClockSkew = TimeSpan.Zero // No tolerance for expiration time
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
    }
}