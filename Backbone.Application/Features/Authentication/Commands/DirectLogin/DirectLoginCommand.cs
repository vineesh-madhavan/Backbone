//Backbone Application/Features/Authentication/Commands/DirectLogin/DirectLoginCommand.cs
using MediatR;

namespace Backbone.Application.Features.Authentication.Commands.DirectLogin
{
    public class DirectLoginCommand : IRequest<DirectLoginResponse>
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

        public DirectLoginCommand() { }

        public DirectLoginCommand(string username, string password, string role)
        {
            Username = username;
            Password = password;
            Role = role;
        }
    }
}
