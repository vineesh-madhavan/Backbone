//Backbone.Application/Features/Authentication/Handlers/LoginCommandHandler.cs
using Backbone.Application.Features.Authentication.Commands.Login;
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Backbone.Application.Features.Authentication.Handlers
{
    //public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    //{
    //    private readonly IJwtService _jwtService;
    //    private readonly ILogger<LoginCommandHandler> _logger;

    //    public LoginCommandHandler(IJwtService jwtService, ILogger<LoginCommandHandler> logger)
    //    {
    //        _jwtService = jwtService;
    //        _logger = logger;
    //    }

    //    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    //    {
    //        try
    //        {
    //            Log.Information("Login attempt for user: {Username}", request.Username);

    //            if (request.Username == "admin" && request.Password == "password")
    //            {
    //                var token = _jwtService.GenerateToken(request.Username);
    //                Log.Information("User {Username} authenticated successfully.", request.Username);
    //                return new LoginResponse(true, token, null);
    //            }

    //            Log.Warning("Failed login attempt for user: {Username}", request.Username);
    //            return new LoginResponse(false, null, "Invalid credentials");
    //        }
    //        catch (Exception ex)
    //        {
    //            Log.Error(ex, "An error occurred during login for user: {Username}", request.Username);
    //            throw;
    //        }
    //    }
    //}
    //public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    //{
    //    private readonly IJwtService _jwtService;
    //    private readonly ILogger<LoginCommandHandler> _logger;

    //    public LoginCommandHandler(
    //        IJwtService jwtService,
    //        ILogger<LoginCommandHandler> logger)
    //    {
    //        _jwtService = jwtService;
    //        _logger = logger;
    //    }

    //    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    //    {
    //        try
    //        {
    //            _logger.LogInformation("Login attempt for user: {Username}", request.Username);

    //            if (request.Username == "admin" && request.Password == "password")
    //            {
    //                var token = _jwtService.GenerateToken(request.Username);
    //                _logger.LogInformation("User {Username} authenticated successfully", request.Username);
    //                return new LoginResponse(true, token, null);
    //            }

    //            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
    //            return new LoginResponse(false, null, "Invalid credentials");
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
    //            throw;
    //        }
    //    }
    //}

    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger<LoginCommandHandler> _logger;
        private readonly IUserRepository _userRepository;

        public LoginCommandHandler(
            IJwtService jwtService,
            ILogger<LoginCommandHandler> logger,
            IUserRepository userRepository)
        {
            _jwtService = jwtService;
            _logger = logger;
            _userRepository = userRepository;
        }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {Username}", request.Username);

                // Validate credentials using existing repository method
                var isValid = await _userRepository.ValidateCredentialsAsync(
                    request.Username,
                    request.Password,
                    cancellationToken);

                if (!isValid)
                {
                    _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
                    return new LoginResponse(false, null, "Invalid credentials");
                }

                // Get user with roles for token generation
                var user = await _userRepository.GetByUsernameAsync(
                    request.Username,
                     true,
                    cancellationToken: cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User not found after credential validation: {Username}", request.Username);
                    return new LoginResponse(false, null, "User not found");
                }

                // Extract roles for token generation
                var roles = user.UserRoleMappings
                    .Select(urm => urm.Role?.RoleName)
                    .Where(roleName => !string.IsNullOrEmpty(roleName))
                    .ToList();

                // Generate token using your existing method
                var token = _jwtService.GenerateToken(user.UserName, roles);

                _logger.LogInformation("User {Username} authenticated successfully", request.Username);
                return new LoginResponse(true, token, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
                throw;
            }
        }
    }
}
