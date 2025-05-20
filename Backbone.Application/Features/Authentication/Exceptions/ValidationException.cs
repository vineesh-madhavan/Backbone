//Backbone.Application.Features.Authentication.Exceptions.ValidationException.cs
using System.Collections.Generic;

namespace Backbone.Application.Shared.Exceptions
{
    public class ValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("Validation failed")
        {
            Errors = errors;
        }
    }
}