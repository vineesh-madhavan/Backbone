// Backbone.Application/DependencyInjection.cs
using Backbone.Application.Features.Authentication.Commands.Login;
using Backbone.Application.Features.Authentication.Handlers;
using Backbone.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Correct registration for MediatR 11.x
        services.AddMediatR(typeof(LoginCommandHandler).Assembly);

        // Add other application services (validation, etc.)
        services.AddValidatorsFromAssembly(typeof(LoginCommandValidator).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>),typeof(ValidationBehavior<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(LoggingBehavior<,>));

        return services;
    }
}