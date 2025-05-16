//Backbone.Api/Middlewares/ExceptionHandlingMiddleware.cs
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var request = context.Request;

            // Structured logging with request context
            _logger.LogError(ex, "Unhandled exception occurred while processing {Method} {Path}",
                request.Method,
                request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        _logger.LogDebug("Handling exception of type {ExceptionType}: {Message}",
            exception.GetType().Name,
            exception.Message);

        object errorResponse;
        int statusCode;

        // Handle FluentValidation exceptions without direct reference
        if (exception.GetType().Name == "ValidationException")
        {
            statusCode = (int)HttpStatusCode.BadRequest;
            var errors = GetValidationErrors(exception); // New helper method
            errorResponse = new { message = "Validation errors occurred", errors };
            _logger.LogWarning("Validation failed: {ValidationErrors}", errors);
        }
        else
        {
            switch (exception)
            {
                case ArgumentNullException ex:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new { message = "Missing required parameter", details = ex.Message };
                    _logger.LogWarning("Bad request: {Message}", ex.Message);
                    break;

                case UnauthorizedAccessException ex:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse = new { message = "Unauthorized access", details = ex.Message };
                    _logger.LogWarning("Unauthorized access attempt: {Message}", ex.Message);
                    break;

                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse = new { message = "An unexpected error occurred", details = exception.Message };
                    _logger.LogError(exception, "Unhandled exception of type {ExceptionType} occurred",
                        exception.GetType().Name);
                    break;
            }
        }

        response.StatusCode = statusCode;
        await response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }

    // Helper method to extract validation errors without dynamic typing
    private static Dictionary<string, string[]> GetValidationErrors(Exception exception)
    {
        var errors = new Dictionary<string, string[]>();
        var errorsProperty = exception.GetType().GetProperty("Errors");

        if (errorsProperty?.GetValue(exception) is IEnumerable<dynamic> errorsCollection)
        {
            foreach (var error in errorsCollection)
            {
                try
                {
                    string propertyName = error.PropertyName ?? "General";
                    string errorMessage = error.ErrorMessage ?? "Validation failed";

                    if (errors.ContainsKey(propertyName))
                    {
                        var existing = errors[propertyName].ToList();
                        existing.Add(errorMessage);
                        errors[propertyName] = existing.ToArray();
                    }
                    else
                    {
                        errors[propertyName] = new[] { errorMessage };
                    }
                }
                catch
                {
                    // Skip malformed error entries
                    continue;
                }
            }
        }
        return errors;
    }
}