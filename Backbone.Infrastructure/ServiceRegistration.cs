//Backbone.Infrastructure/ServiceRegistration.cs
using Backbone.Core.Interfaces;
using Backbone.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace Backbone.Infrastructure
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            try
            {
                var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("ServiceRegistration");

                logger?.LogInformation("Starting infrastructure services registration");

                RegisterJwtServices(services, config, logger);
                RegisterAuthentication(services, config, logger);

                logger?.LogInformation("Completed infrastructure services registration");
                return services;
            }
            catch (Exception ex)
            {
                // Fallback logging if DI isn't fully set up yet
                Console.WriteLine($"Failed to register infrastructure services: {ex}");
                throw new ServiceRegistrationException("Failed to configure infrastructure services", ex);
            }
        }

        private static void RegisterJwtServices(IServiceCollection services, IConfiguration config, ILogger logger)
        {
            try
            {
                logger?.LogDebug("Registering JWT services");
                services.AddSingleton<IJwtService, JwtService>();
                logger?.LogInformation("Registered JWT service");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to register JWT services");
                throw;
            }
        }

        private static void RegisterAuthentication(IServiceCollection services, IConfiguration config, ILogger logger)
        {
            try
            {
                logger?.LogDebug("Configuring authentication services");

                var jwtSettings = config.GetSection("Jwt");
                if (jwtSettings == null || !jwtSettings.Exists())
                {
                    var error = "JWT settings not found in configuration";
                    logger?.LogError(error);
                    throw new ConfigurationException(error);
                }

                var key = config["Jwt:Key"];
                if (string.IsNullOrWhiteSpace(key))
                {
                    var error = "JWT signing key is missing in configuration";
                    logger?.LogError(error);
                    throw new ConfigurationException(error);
                }

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        try
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = config["Jwt:Issuer"],
                                ValidAudience = config["Jwt:Audience"],
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                            };

                            logger?.LogInformation("Configured JWT bearer authentication");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "Failed to configure JWT bearer options");
                            throw;
                        }
                    });

                services.AddAuthorization();
                logger?.LogInformation("Added authorization services");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to configure authentication services");
                throw;
            }
        }
    }

    public class ServiceRegistrationException : Exception
    {
        public ServiceRegistrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message)
            : base(message)
        {
        }
    }
}
