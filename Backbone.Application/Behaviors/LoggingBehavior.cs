// Backbone.Application/Behaviors/LoggingBehavior.cs
using MediatR;
using Serilog;

public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        Log.Information("Handling {RequestName}: {@Request}",
            requestName, request);

        var response = await next();

        Log.Information("Handled {RequestName}. Response: {@Response}",
            requestName, response);

        return response;
    }
}