//Backbone.Api/Program.cs
using Backbone.Api.Middleware;
using Backbone.Application;
using Backbone.Application.Features.Authentication.Commands.Login;
using Backbone.Core.Settings;
using Backbone.Infrastructure;
using Backbone.Infrastructure.Extensions;
using Backbone.Infrastructure.Logging;
using Backbone.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Configure Serilog using our custom extension
builder.Host.UseCustomSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Backbone API",
        Version = "v1",
        Description = "Backbone API with custom endpoints"
    });

    // This helps Swagger discover the endpoints
    c.TagActionsBy(api => new[] { api.GroupName });
    c.DocInclusionPredicate((name, api) => true);
});

// Application services
builder.Services.AddApplicationServices();
builder.Services.AddApplication();
builder.Services.AddInfrastructureServices(config);

// Database Context with pooling, retry policy, AND logging
builder.Services.AddDbContextPool<ApplicationDbContext>((provider, options) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    })
        .UseLoggerFactory(provider.GetRequiredService<ILoggerFactory>());

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
}, poolSize: 128);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("Auth"));

// Fluent Validation
builder.Services.AddValidatorsFromAssemblyContaining<LoginCommandValidator>();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireMasterRole", policy => policy.RequireRole("Master", "Admin"));
    options.AddPolicy("RequireSubscriberRole", policy => policy.RequireRole("Subscriber", "Master", "Admin"));
});

// Build the application
var app = builder.Build();

// Log the listening URLs
var urls = app.Urls;
Console.WriteLine($"API running on: {string.Join(", ", urls)}");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Backbone API V1");
        c.RoutePrefix = string.Empty; // Serve at root
    });
}

// Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging(); // Log HTTP requests
app.UseMigrations(); // Apply database migrations

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<ImpersonationMiddleware>();
app.UseAuthorization();

app.MapAuthenticationEndpoints();

app.Run();

// Database Sink Provider implementation
namespace Backbone.Api
{
    public class DatabaseSinkProvider : ILogEventSink, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseSinkProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sink = scope.ServiceProvider.GetRequiredService<DatabaseSink>();
                sink.Emit(logEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to emit log event: {ex}");
            }
        }

        public void Dispose()
        {
            // Cleanup resources if needed
        }
    }
}