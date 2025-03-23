using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backbone.Infrastructure.Services;
using Backbone.Domain.Interfaces;  // ✅ Import IJwtService
using MediatR;
using Microsoft.Extensions.Configuration;
using Backbone.Application.Commands;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IJwtService, JwtService>();  // ✅ Ensure JwtService is correctly registered
        services.AddMediatR(typeof(LoginCommandHandler).Assembly);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSettings = config.GetSection("Jwt");
                if (jwtSettings == null)
                {
                    throw new ArgumentNullException(nameof(jwtSettings), "JWT settings not found in configuration.");
                }
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? throw new ArgumentNullException(nameof(config))))
                };
            });
        services.AddAuthorization();
        return services;
    }
}
