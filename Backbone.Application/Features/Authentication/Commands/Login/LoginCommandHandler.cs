﻿//Backbone.Application.Features.Authentication.Commands.Login.LoginCommandHandler.cs

using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Backbone.Application.Features.Authentication.Commands.Login
{
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
            using var _ = _logger.BeginScope(new { request.Username, RequestId = Guid.NewGuid() });

            try
            {
                _logger.LogInformation("Login attempt initiated");
                _logger.LogDebug("Validating credentials for {Username}", request.Username);

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
                var user = await _userRepository.GetByUsernameAsync(
                    request.Username,
                    true,
                    cancellationToken: cancellationToken);

                if (user == null)
                {
                    _logger.LogError("User not found after successful credential validation for {Username}", request.Username);
                    return new LoginResponse(false, null, "System error");
                }

                var roles = user.UserRoleMappings?
                    .Select(urm => urm.Role?.RoleName)
                    .Where(roleName => !string.IsNullOrEmpty(roleName))
                    .ToList() ?? new List<string>();

                if (!roles.Any())
                {
                    _logger.LogWarning("User {Username} has no roles assigned", request.Username);
                    return new LoginResponse(false, null, "User has no roles assigned");
                }

                // Handle single-role user
                if (roles.Count == 1)
                {
                    _logger.LogDebug("Generating final JWT token for single-role user {Username}", request.Username);
                    var token = _jwtService.GenerateRoleSpecificToken(user.UserName, roles[0], roles);
                    return new LoginResponse(true, token, null, false, roles, user.Id);
                }

                // Handle multi-role user
                _logger.LogDebug("Generating initial JWT token for multi-role user {Username}", request.Username);
                var initialToken = _jwtService.GenerateInitialToken(user.UserName, roles);
                return new LoginResponse(true, initialToken, null, true, roles, user.Id);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("Login operation canceled: {Message}", ex.Message);
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