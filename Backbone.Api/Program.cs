using MediatR;
using Microsoft.AspNetCore.Authorization;
using Backbone.Application;
using Backbone.Application.Commands;
using Backbone.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api_log.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(config);

// 🔹 Add Controllers and Exception Filter Here
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExceptionFilter>();
});

var app = builder.Build();

// Use Middleware for Exception Handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging(); // Log incoming requests

// Middleware
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (LoginCommand command, IMediator mediator) => await mediator.Send(command));
app.MapGet("/secure", [Authorize] () => "This is a secure endpoint");

app.Run();
