//Backbone Application/Features/Authentication/Commands/DirectLogin/DirectLoginCommandHandler.cs
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Backbone.Application.Features.Authentication.Commands.DirectLogin
{
    public class DirectLoginCommandHandler : IRequestHandler<DirectLoginCommand, DirectLoginResponse>
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger<DirectLoginCommandHandler> _logger;
        private readonly IUserRepository _userRepository;

        public DirectLoginCommandHandler(
            IJwtService jwtService,
            ILogger<DirectLoginCommandHandler> logger,
            IUserRepository userRepository)
        {
            _jwtService = jwtService;
            _logger = logger;
            _userRepository = userRepository;
        }

        public async Task<DirectLoginResponse> Handle(DirectLoginCommand request, CancellationToken cancellationToken)
        {
            using var _ = _logger.BeginScope(new { request.Username, RequestId = Guid.NewGuid() });

            try
            {
                _logger.LogInformation("Direct login attempt initiated for {Username}", request.Username);
                _logger.LogDebug("Validating credentials for {Username}", request.Username);

                var isValid = await _userRepository.ValidateCredentialsAsync(
                    request.Username,
                    request.Password,
                    cancellationToken);

                if (!isValid)
                {
                    _logger.LogWarning("Invalid credentials provided for {Username}", request.Username);
                    return new DirectLoginResponse(false, null, "Invalid credentials");
                }

                _logger.LogDebug("Credentials validated, retrieving user details");
                var user = await _userRepository.GetByUsernameAsync(
                    request.Username,
                    true,
                    cancellationToken: cancellationToken);
                 
                if (user == null)
                {
                    _logger.LogError("User not found after successful credential validation for {Username}", request.Username);
                    return new DirectLoginResponse(false, null, "System error");
                }

                var roles = user.UserRoleMappings?
                    .Select(urm => urm.Role?.RoleName)
                    .Where(roleName => !string.IsNullOrEmpty(roleName))
                    .ToList() ?? new List<string>();

                if (!roles.Any())
                {
                    _logger.LogWarning("User {Username} has no roles assigned", request.Username);
                    return new DirectLoginResponse(false, null, "User has no roles assigned");
                }

                if (!roles.Contains(request.Role))
                {
                    _logger.LogWarning("User {Username} doesn't have role {Role}", request.Username, request.Role);
                    return new DirectLoginResponse(false, null, $"User doesn't have the '{request.Role}' role");
                }

                _logger.LogDebug("Generating JWT token for {Username} with role {Role}",
                    request.Username, request.Role);
                var token = _jwtService.GenerateRoleSpecificToken(user.UserName, request.Role, roles);

                _logger.LogInformation("Successful direct login for {Username}", request.Username);
                return new DirectLoginResponse(true, token, null, request.Role, roles);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("Direct login operation canceled: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during direct login for {Username}", request.Username);
                throw new AuthenticationException("Direct login failed due to system error");
            }
        }
    }
}