using MediatR;

namespace Backbone.Application.Features.Authentication.Commands.Login
{
    public record LoginCommand(string Username, string Password) : IRequest<LoginResponse>;
}
