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

namespace Backbone.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration config, ILogger<JwtService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateToken(string username, IEnumerable<string> roles)
        {
            _logger.LogInformation("Generating JWT for {Username} with roles: {Roles}",
                username, string.Join(",", roles));

            try
            {
                // Input validation
                if (string.IsNullOrEmpty(username))
                {
                    throw new ArgumentException("Username cannot be null or empty", nameof(username));
                }

                if (roles == null || !roles.Any())
                {
                    _logger.LogWarning("No roles provided for user {Username}", username);
                    roles = new[] { "DefaultRole" }; // Provide a default role if empty
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

                if (key.Length < 32) // Minimum 256-bit key for HS256
                {
                    throw new ArgumentException("JWT key must be at least 256 bits (32 bytes)");
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, username),
                    new Claim(JwtRegisteredClaimNames.Email, username)
                };

                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(15),
                    Issuer = _config["Jwt:Issuer"],
                    Audience = _config["Jwt:Audience"],
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = tokenHandler.WriteToken(token);

                _logger.LogDebug("Successfully generated JWT for {Username} with ID {TokenId} expiring at {Expiration}",
                    username, token.Id, token.ValidTo);

                return jwtToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate JWT for {Username}", username);
                throw new SecurityTokenException("Token generation failed", ex);
            }
        }
    }
}