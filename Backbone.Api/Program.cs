//Backbone.Api/Program.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Backbone.Application;
using Backbone.Application.Commands;
using Backbone.Infrastructure;
using Backbone.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Use Serilog with configuration from appsettings.json
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(config);

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
