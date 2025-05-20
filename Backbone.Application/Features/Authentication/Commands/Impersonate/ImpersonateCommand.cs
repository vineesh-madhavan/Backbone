//Backbone.Application/Features/Authentication/Commands/Impersonate/ImpersonateCommand.cs

using MediatR;

namespace Backbone.Application.Features.Authentication.Commands.Impersonate
{
    public class ImpersonateCommand : IRequest<ImpersonateResponse>
    {
        public string Username { get; set; }
        public string Role { get; set; } // Optional role to impersonate
    }
}