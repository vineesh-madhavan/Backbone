//Backbone Application/Features/Authentication/Commands/SwitchRole/SwitchRoleCommandHandler.cs
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Backbone.Application.Features.Authentication.Exceptions;

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
                var currentUserId = _currentUserService.UserId;
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
                var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("User {UserId} not found or inactive during role switch", currentUserId);
                    return new SwitchRoleResponse(false, null, "User account is not active");
                }

                // Create claims (preserve any existing claims)
                var existingClaims = _currentUserService.FindClaims(null).ToList();
                var claimsToKeep = existingClaims
                    .Where(c => c.Type != ClaimTypes.Role &&
                               c.Type != "current_role" &&
                               c.Type != JwtRegisteredClaimNames.Jti)
                    .ToList();

                // Add role-specific claims
                var claims = new List<Claim>(claimsToKeep)
                {
                    new Claim("current_role", request.NewRole)
                };

                // Generate new token
                var token = _jwtService.GenerateRoleSpecificToken(
                    currentUsername,
                    request.NewRole,
                    availableRoles,
                    claims);

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