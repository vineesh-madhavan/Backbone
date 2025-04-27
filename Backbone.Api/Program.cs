//Backbone.Api/Program.cs
using Backbone.Application;
using Backbone.Application.Features.Authentication.Commands.Login;
using Backbone.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Polly;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Use Serilog with configuration from appsettings.json
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    );



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
