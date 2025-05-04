//Backbone.Api/Program.cs
using Backbone.Application;
using Backbone.Application.Features.Authentication.Commands.Login;
using Backbone.Infrastructure.Logging;
using Backbone.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

//// Use Serilog with configuration from appsettings.json
//builder.Host.UseSerilog((context, services, configuration) => configuration
//    .ReadFrom.Configuration(context.Configuration)
//    .ReadFrom.Services(services)
//    .Enrich.FromLogContext()  
//    );

builder.Host
    .UseCustomSerilog()  // From Infrastructure
    .ConfigureServices(services => {
        // Remove AddSerilogLogging() if not defined
        services.AddLogging(logging =>
        {
            logging.AddSerilog();
        });
    });

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationServices();
builder.Services.AddApplication();
builder.Services.AddInfrastructureServices(config);
//builder.Services.AddHealthChecks()
//    .AddNpgSql(configuration.GetConnectionString("DefaultConnection"));

//For DBContext
// Essential registration (minimum)
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Recommended production-ready registration:
builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null); // Explicitly pass null for error codes
        }),
    poolSize: 128);



var app = builder.Build();




// Use Middleware for Exception Handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging(); // Log incoming requests


// Apply migrations at startup
app.UseMigrations();

// Middleware
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (LoginCommand command, IMediator mediator) => await mediator.Send(command));
app.MapGet("/secure", [Authorize] () => "This is a secure endpoint");

app.Run();

namespace Backbone.Api
{
    public class DatabaseSinkProvider : ILogEventSink, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseSinkProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            using var scope = _serviceProvider.CreateScope();
            var sink = scope.ServiceProvider.GetRequiredService<DatabaseSink>();
            sink.Emit(logEvent);
        }

        public void Dispose() { }
    }
}