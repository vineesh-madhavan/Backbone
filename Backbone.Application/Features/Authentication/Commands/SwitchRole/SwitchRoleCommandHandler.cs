//Backbone Application/Features/Authentication/Commands/SwitchRole/SwitchRoleCommandHandler.cs
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Backbone.Application.Features.Authentication.Commands.SwitchRole
{
    public class SwitchRoleCommandHandler : IRequestHandler<SwitchRoleCommand, SwitchRoleResponse>
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger<SwitchRoleCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserRepository _userRepository;

        public SwitchRoleCommandHandler(
            IJwtService jwtService,
            ILogger<SwitchRoleCommandHandler> logger,
            ICurrentUserService currentUserService,
            IUserRepository userRepository)
        {
            _jwtService = jwtService;
            _logger = logger;
            _currentUserService = currentUserService;
            _userRepository = userRepository;
        }

        public async Task<SwitchRoleResponse> Handle(SwitchRoleCommand request, CancellationToken cancellationToken)
        {
            using var _ = _logger.BeginScope(new { RequestId = Guid.NewGuid() });

            try
            {
                var currentUsername = _currentUserService.Username;
                //var currentUserId = _currentUserService.UserId;
                var previousRole = _currentUserService.CurrentRole;
                var availableRoles = _currentUserService.AvailableRoles.ToList();

                _logger.LogDebug("Attempting to switch role for user {Username} from {PreviousRole} to {NewRole}",
                    currentUsername, previousRole, request.NewRole);

                // Validate user is authenticated
                if (!_currentUserService.IsAuthenticated)
                {
                    _logger.LogWarning("Unauthenticated user attempted to switch roles");
                    return new SwitchRoleResponse(false, null, "User is not authenticated");
                }

                // Validate user has multiple roles
                if (availableRoles.Count <= 1)
                {
                    _logger.LogWarning("User {Username} attempted to switch roles but only has one role", currentUsername);
                    return new SwitchRoleResponse(false, null, "User only has one assigned role");
                }

                // Validate requested role exists
                if (!availableRoles.Contains(request.NewRole))
                {
                    _logger.LogWarning("User {Username} attempted to switch to invalid role {Role}",
                        currentUsername, request.NewRole);
                    return new SwitchRoleResponse(false, null, $"User doesn't have the '{request.NewRole}' role");
                }

                // Get user to verify active status
                var user = await _userRepository.GetByUsernameAsync(currentUsername,false, cancellationToken);
                if (user == null ) // check isActive logic here
                {
                    _logger.LogWarning("User {UserId} not found or inactive during role switch", currentUsername);
                    return new SwitchRoleResponse(false, null, "User account is not active");
                }

                // Get and filter existing c laims
                var existingClaims = _currentUserService.GetAllClaims();
                var claimsToKeep = _jwtService.FilterClaims(existingClaims,
                    ClaimTypes.Role,
                    "current_role",
                    JwtRegisteredClaimNames.Jti);

                // Add new role claim
                var newClaims = claimsToKeep.Append(new Claim("current_role", request.NewRole));

                // Generate token using the new method
                var token = _jwtService.GenerateTokenWithClaims(
                    currentUsername,
                    new[] { request.NewRole },
                    newClaims);

                _logger.LogInformation(
                    "User {Username} successfully switched from role {PreviousRole} to {NewRole}",
                    currentUsername,
                    previousRole,
                    request.NewRole);

                return new SwitchRoleResponse(
                    true,
                    token,
                    null,
                    previousRole,
                    request.NewRole,
                    availableRoles);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("Role switch operation canceled: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during role switch");
                return new SwitchRoleResponse(false, null, "Role switch failed due to system error");
            }
        }
    }
}