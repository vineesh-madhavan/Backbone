//Backbone.Application/Features/Authentication/Commands/Login/LoginCommand.cs
using MediatR;

namespace Backbone.Application.Features.Authentication.Commands.Login
{
    public class LoginCommand : IRequest<LoginResponse>
    {
        public string Username { get; set; }
        public string Password { get; set; }

        // Add parameterless constructor for testing
        public LoginCommand() { }

        // Add parameterized constructor for convenience
        public LoginCommand(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
