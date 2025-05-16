// Backbone.Infrastructure/Logging/DatabaseSink.cs
using Backbone.Infrastructure.Data;
using Backbone.Infrastructure.Entities;
using Backbone.Infrastructure.Persistence;
using Backbone.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

public class DatabaseSink : ILogEventSink
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSink> _logger;

    public DatabaseSink(IServiceProvider serviceProvider, ILogger<DatabaseSink> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Emit(LogEvent logEvent)
    {
        using var scope = _logger.BeginScope(new { LogEventId = Guid.NewGuid() });

        try
        {
            _logger.LogDebug("Starting to process log event");
            var stopwatch = Stopwatch.StartNew();

            using var serviceScope = _serviceProvider.CreateScope();
            var services = serviceScope.ServiceProvider;

            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var currentUserService = services.GetService<ICurrentUserService>();
            var httpContextAccessor = services.GetService<IHttpContextAccessor>();

            // Extract user context safely
            var (jwtTokenId, tokenExpiry, username, roles) = ExtractUserContext(httpContextAccessor, currentUserService);

            // Create log entry
            var logEntry = CreateLogEntry(logEvent, jwtTokenId, tokenExpiry, username, roles);

            // Save to database
            SaveLogEntry(dbContext, logEntry);

            stopwatch.Stop();
            _logger.LogDebug("Successfully processed log event in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process log event");
            // Consider fallback logging mechanism here
        }
    }

    private (string jwtTokenId, DateTime? tokenExpiry, string username, string roles)
        ExtractUserContext(IHttpContextAccessor httpContextAccessor, ICurrentUserService currentUserService)
    {
        try
        {
            _logger.LogDebug("Extracting user context");

            var jwtTokenId = "No-Token";
            DateTime? tokenExpiry = null;
            var username = "Anonymous";
            var roles = "None";

            // Try to get from HTTP context first
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                var token = authHeader?.Split(' ').Last();

                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        if (handler.CanReadToken(token))
                        {
                            var jwtToken = handler.ReadJwtToken(token);
                            jwtTokenId = jwtToken.Id;
                            tokenExpiry = jwtToken.ValidTo;

                            username = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? username;
                            roles = string.Join(",", jwtToken.Claims
                                .Where(c => c.Type == ClaimTypes.Role)
                                .Select(c => c.Value));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse JWT token");
                        jwtTokenId = "Invalid-Token";
                    }
                }
            }

            // Override with current user service if available
            if (currentUserService != null)
            {
                username = currentUserService.Username ?? username;
                roles = currentUserService.Roles.Any()
                    ? string.Join(",", currentUserService.Roles)
                    : roles;
            }

            _logger.LogTrace("Extracted user context: {Username}, Roles: {Roles}", username, roles);
            return (jwtTokenId, tokenExpiry, username, roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user context");
            return ("Error-Token", null, "Error-User", "Error-Roles");
        }
    }

    private ApplicationLog CreateLogEntry(
        LogEvent logEvent,
        string jwtTokenId,
        DateTime? tokenExpiry,
        string username,
        string roles)
    {
        try
        {
            _logger.LogDebug("Creating log entry");

            logEvent.Properties.TryGetValue("SourceContext", out var sourceContext);
            logEvent.Properties.TryGetValue("FilePath", out var filePath);
            logEvent.Properties.TryGetValue("MemberName", out var memberName);
            logEvent.Properties.TryGetValue("LineNumber", out var lineNumber);

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
                Properties = SafeSerializeProperties(logEvent, username, roles, jwtTokenId, tokenExpiry),
                LogEvent = logEvent.ToString(),
                UserName = username,
                UserRoles = roles,
                JwtTokenId = jwtTokenId,
                TokenExpiry = tokenExpiry
            };

            _logger.LogTrace("Created log entry for {MessageTemplate}", log.MessageTemplate);
            return log;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating log entry");
            throw;
        }
    }

    private string SafeSerializeProperties(
        LogEvent logEvent,
        string username,
        string roles,
        string jwtTokenId,
        DateTime? tokenExpiry)
    {
        try
        {
            return JsonConvert.SerializeObject(new
            {
                OriginalProperties = logEvent.Properties.ToDictionary(
                    k => k.Key,
                    v => v.Value.ToString()),
                User = new
                {
                    Username = username,
                    Roles = roles,
                    JwtTokenId = jwtTokenId,
                    TokenExpiry = tokenExpiry
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize log properties");
            return "{\"error\":\"Failed to serialize properties\"}";
        }
    }

    private void SaveLogEntry(ApplicationDbContext dbContext, ApplicationLog logEntry)
    {
        try
        {
            _logger.LogDebug("Saving log entry to database");

            dbContext.ApplicationLogs.Add(logEntry);
            var changes = dbContext.SaveChanges();

            _logger.LogTrace("Saved log entry with ID {LogId}", logEntry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save log entry to database");
            // Consider implementing a retry mechanism or fallback storage
            throw;
        }
    }
}