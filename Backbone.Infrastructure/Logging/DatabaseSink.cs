// Backbone.Infrastructure/Logging/DatabaseSink.cs
using Backbone.Infrastructure.Data;
using Backbone.Infrastructure.Entities;
using Backbone.Infrastructure.Persistence;
using Backbone.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using System;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

public class DatabaseSink : ILogEventSink
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseSink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var currentUserService = scope.ServiceProvider.GetService<ICurrentUserService>();
        var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();

        // Extract standard log properties
        logEvent.Properties.TryGetValue("SourceContext", out var sourceContext);
        logEvent.Properties.TryGetValue("FilePath", out var filePath);
        logEvent.Properties.TryGetValue("MemberName", out var memberName);
        logEvent.Properties.TryGetValue("LineNumber", out var lineNumber);

        // Get JWT token information
        var jwtTokenId = "No-Token";
        DateTime? tokenExpiry = null;
        var username = "Anonymous";
        var roles = "None";

        var httpContext = httpContextAccessor?.HttpContext;
        if (httpContext != null)
        {
            // Get token from Authorization header
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            var token = authHeader?.Split(' ').Last();

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    jwtTokenId = jwtToken.Id;
                    tokenExpiry = jwtToken.ValidTo;

                    // Get username and roles from claims if currentUserService isn't available
                    username = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "Anonymous";
                    roles = string.Join(",", jwtToken.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value));
                }
                catch
                {
                    // Token is invalid or malformed
                    jwtTokenId = "Invalid-Token";
                }
            }
        }

        // Use currentUserService if available (more reliable for username/roles)
        if (currentUserService != null)
        {
            username = currentUserService.Username ?? username;
            roles = currentUserService.Roles.Any()
                ? string.Join(",", currentUserService.Roles)
                : roles;
        }

        var log = new ApplicationLog
        {
            ProjectName = sourceContext?.ToString()?.Split('.')[0],
            SourceFile = System.IO.Path.GetFileName(filePath?.ToString()?.Trim('"') ?? ""),
            MethodName = memberName?.ToString()?.Trim('"'),
            LineNumber = lineNumber != null ? int.Parse(lineNumber.ToString()) : (int?)null,

            Message = logEvent.RenderMessage(),
            MessageTemplate = logEvent.MessageTemplate.Text,
            Level = logEvent.Level.ToString(),
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Exception = logEvent.Exception?.ToString(),
            Properties = JsonConvert.SerializeObject(new
            {
                OriginalProperties = logEvent.Properties,
                User = new
                {
                    Username = username,
                    Roles = roles,
                    JwtTokenId = jwtTokenId,
                    TokenExpiry = tokenExpiry
                }
            }),
            LogEvent = logEvent.ToString(),
            UserName = username,
            UserRoles = roles,
            JwtTokenId = jwtTokenId,
            TokenExpiry = tokenExpiry
        };

        dbContext.ApplicationLogs.Add(log);
        dbContext.SaveChanges();
    }
}