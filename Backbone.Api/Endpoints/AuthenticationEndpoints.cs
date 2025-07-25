//Backbone.Api.Endpoints.AuthenticationEndpoints.cs

using Backbone.Application.Features.Authentication.Commands.Impersonate;
using Backbone.Application.Features.Authentication.Commands.Login;
using Backbone.Application.Features.Authentication.Exceptions;
using Backbone.Application.Shared.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public static class AuthenticationEndpoints
{
    public static RouteGroupBuilder MapAuthenticationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Authentication")
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        // Impersonation endpoint
        group.MapPost("/impersonate", Impersonate)
            .Produces<ImpersonateResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization("AdminPolicy");

        return group;
    }

    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    private static async Task<IResult> Login(
        LoginCommand command,
        IMediator mediator,
        ILogger<Program> logger)
    {
        logger.LogInformation("Login attempt for username: {Username}", command.Username);

        try
        {
            var result = await mediator.Send(command);

            if (!result.Success)
            {
                logger.LogWarning("Failed login attempt for username: {Username}. Reason: {Error}",
                    command.Username, result.Error);
                return Results.Unauthorized();
            }

            logger.LogInformation("Successful login for username: {Username}", command.Username);
            return Results.Ok(result);
        }
        catch (Backbone.Application.Features.Authentication.Exceptions.AuthenticationException ex)
        {
            logger.LogWarning(ex, "Authentication failed for username: {Username}", command.Username);
            return Results.Unauthorized();
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation failed for username: {Username}. Errors: {@Errors}",
                command.Username, ex.Errors);
            return Results.BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during login for username: {Username}", command.Username);
            return Results.Problem("An unexpected error occurred");
        }


    }

    [ProducesResponseType(typeof(ImpersonateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    private static async Task<IResult> Impersonate(
            ImpersonateCommand command,
            IMediator mediator,
            ILogger<Program> logger)
    {
        logger.LogInformation("Impersonation attempt for username: {Username}, role: {Role}",
            command.Username, command.Role);

        try
        {
            var result = await mediator.Send(command);
            logger.LogInformation("Successful impersonation of {Username} with role {Role}",
                command.Username, command.Role ?? "all roles");
            return Results.Ok(result);
        }
        catch (AuthenticationException ex)
        {
            logger.LogWarning("Authentication failed during impersonation: {Message}", ex.Message);
            return Results.Unauthorized();
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation failed during impersonation: {Errors}", ex.Errors);
            return Results.BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during impersonation");
            return Results.Problem("An unexpected error occurred");
        }
    }
}