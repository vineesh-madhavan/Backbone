//Backbone.Application.Features.Authentication.Exceptions.AuthenticationException.cs
namespace Backbone.Application.Features.Authentication.Exceptions
{
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message) { }
        public AuthenticationException(string message, Exception inner) : base(message, inner) { }
    }
}