namespace Backbone.Application.Features.Authentication.Commands.Login
{
    public record LoginResponse(bool Success, string? Token, string? ErrorMessage);
}
