//Backbone.Application/Features/Authentication/Commands/Login/LoginResponse.cs
namespace Backbone.Application.Features.Authentication.Commands.Login
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Error { get; set; }

        // Add parameterless constructor for testing
        public LoginResponse() { }

        public LoginResponse(bool success, string token, string error)
        {
            Success = success;
            Token = token;
            Error = error;
        }
    }
}