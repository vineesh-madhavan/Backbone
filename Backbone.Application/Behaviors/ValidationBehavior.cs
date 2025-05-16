// Backbone.Application/Behaviors/ValidationBehavior.cs
//using FluentValidation;
//using MediatR;
//using Microsoft.Extensions.Logging;
//using Serilog;
//using Serilog.Context;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Backbone.Application.Behaviors
//{
//    public sealed class ValidationBehavior<TRequest, TResponse>
//        : IPipelineBehavior<TRequest, TResponse>
//        where TRequest : IRequest<TResponse>
//    {
//        private readonly IEnumerable<IValidator<TRequest>> _validators;
//        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

//        public ValidationBehavior(
//            IEnumerable<IValidator<TRequest>> validators,
//            ILogger<ValidationBehavior<TRequest, TResponse>> logger)
//        {
//            _validators = validators;
//            _logger = logger;
//        }

//        public async Task<TResponse> Handle(
//            TRequest request,
//            RequestHandlerDelegate<TResponse> next,
//            CancellationToken cancellationToken)
//        {
//            var requestName = typeof(TRequest).Name;

//            using (LogContext.PushProperty("RequestType", requestName))
//            {
//                _logger.LogDebug("Validating request {RequestName}", requestName);

//                if (!_validators.Any())
//                {
//                    _logger.LogDebug("No validators found for {RequestName}", requestName);
//                    return await next();
//                }

//                var context = new ValidationContext<TRequest>(request);
//                var validationResults = await Task.WhenAll(
//                    _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

//                var failures = validationResults
//                    .SelectMany(result => result.Errors)
//                    .Where(f => f != null)
//                    .ToList();

//                if (failures.Count != 0)
//                {
//                    _logger.LogWarning("Validation failed for {RequestName} with {ErrorCount} errors: {@ValidationErrors}",
//                        requestName,
//                        failures.Count,
//                        failures.GroupBy(e => e.PropertyName)
//                                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

//                    throw new ValidationException(failures);
//                }

//                _logger.LogDebug("Validation successful for {RequestName}", requestName);
//                return await next();
//            }
//        }
//    }
//}

using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backbone.Application.Behaviors
{
    public sealed class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

        public ValidationBehavior(
            IEnumerable<IValidator<TRequest>> validators,
            ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _validators = validators;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogDebug("Validating request {RequestName}", requestName);

            if (!_validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);
            var failures = _validators
                .Select(v => v.Validate(context))
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                _logger.LogWarning("Validation failed for {RequestName} with {ErrorCount} errors",
                    requestName, failures.Count);
                throw new ValidationException(failures);
            }

            return await next();
        }
    }
}