//Backbone.Application/Features/Authentication/Handlers/LoginCommandHandler.cs
using Backbone.Application.Features.Authentication.Commands.Login;
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Authentication;

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

        //    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        //    {
        //        try
        //        {
        //            _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        //            // Validate credentials using existing repository method
        //            var isValid = await _userRepository.ValidateCredentialsAsync(
        //                request.Username,
        //                request.Password,
        //                cancellationToken);

        //            if (!isValid)
        //            {
        //                _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
        //                return new LoginResponse(false, null, "Invalid credentials");
        //            }

        //            // Get user with roles for token generation
        //            var user = await _userRepository.GetByUsernameAsync(
        //                request.Username,
        //                 true,
        //                cancellationToken: cancellationToken);

        //            if (user == null)
        //            {
        //                _logger.LogWarning("User not found after credential validation: {Username}", request.Username);
        //                return new LoginResponse(false, null, "User not found");
        //            }

        //            // Extract roles for token generation
        //            var roles = user.UserRoleMappings
        //                .Select(urm => urm.Role?.RoleName)
        //                .Where(roleName => !string.IsNullOrEmpty(roleName))
        //                .ToList();

        //            // Generate token using your existing method
        //            var token = _jwtService.GenerateToken(user.UserName, roles);

        //            _logger.LogInformation("User {Username} authenticated successfully", request.Username);
        //            return new LoginResponse(true, token, null);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
        //            throw;
        //        }
        //    }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // Add operation context logging
            using var _ = _logger.BeginScope(new { request.Username, RequestId = Guid.NewGuid() });

            try
            {
                _logger.LogInformation("Login attempt initiated");
                _logger.LogDebug("Validating credentials for {Username}", request.Username);

                // Validate credentials
                var isValid = await _userRepository.ValidateCredentialsAsync(
                    request.Username,
                    request.Password,
                    cancellationToken);

                if (!isValid)
                {
                    _logger.LogWarning("Invalid credentials provided for {Username}", request.Username);
                    return new LoginResponse(false, null, "Invalid credentials");
                }

                _logger.LogDebug("Credentials validated, retrieving user details");

                // Get user with roles
                var user = await _userRepository.GetByUsernameAsync(
                    request.Username,
                    true,
                    cancellationToken: cancellationToken);

                if (user == null)
                {
                    _logger.LogError("User not found after successful credential validation for {Username}", request.Username);
                    return new LoginResponse(false, null, "System error");
                }

                // Extract roles with null check
                var roles = user.UserRoleMappings?
                    .Select(urm => urm.Role?.RoleName)
                    .Where(roleName => !string.IsNullOrEmpty(roleName))
                    .ToList() ?? new List<string?>();

                if (!roles.Any())
                {
                    _logger.LogWarning("User {Username} has no roles assigned", request.Username);
                }

                _logger.LogDebug("Generating JWT token for {Username} with roles: {Roles}",
                    request.Username,
                    string.Join(", ", roles));

                var token = _jwtService.GenerateToken(user.UserName, roles!);

                _logger.LogInformation("Successful authentication for {Username}", request.Username);
                _logger.LogDebug("Generated token expires at {Expiration}",
                    DateTime.UtcNow.AddMinutes(15)); // Match your JWT expiration

                return new LoginResponse(true, token, null);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("Login operation canceled for {Username}: {Message}",
                    request.Username, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during login for {Username}", request.Username);
                throw new AuthenticationException("Login failed due to system error");
            }
        }
    }
}
