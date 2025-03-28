using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Backbone.Domain.Interfaces;
using Serilog;

namespace Backbone.Application.Commands
{
    public record LoginCommand(string Username, string Password) : IRequest<LoginResponse>;

    public record LoginResponse(bool Success, string? Token, string? ErrorMessage);

    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(IJwtService jwtService, ILogger<LoginCommandHandler> logger)
        {
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                Log.Information("Login attempt for user: {Username}", request.Username);

                if (request.Username == "admin" && request.Password == "password")
                {
                    var token = _jwtService.GenerateToken(request.Username);
                    Log.Information("User {Username} authenticated successfully.", request.Username);
                    return new LoginResponse(true, token, null);
                }

                Log.Warning("Failed login attempt for user: {Username}", request.Username);
                return new LoginResponse(false, null, "Invalid credentials");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during login for user: {Username}", request.Username);
                throw;
            }
        }
    }
}
