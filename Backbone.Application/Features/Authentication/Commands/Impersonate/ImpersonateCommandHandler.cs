//Backbone.Application/Features/Authentication/Commands/Impersonate/ImpersonateCommandHandler.cs

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

namespace Backbone.Application.Features.Authentication.Commands.Impersonate
{
    public class ImpersonateCommandHandler : IRequestHandler<ImpersonateCommand, ImpersonateResponse>
    {
        private readonly IJwtService _jwtService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ImpersonateCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public ImpersonateCommandHandler(
            IJwtService jwtService,
            IUserRepository userRepository,
            ILogger<ImpersonateCommandHandler> logger,
            ICurrentUserService currentUserService)
        {
            _jwtService = jwtService;
            _userRepository = userRepository;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<ImpersonateResponse> Handle(ImpersonateCommand request, CancellationToken cancellationToken)
        {
            using var _ = _logger.BeginScope(new { RequestId = Guid.NewGuid() });

            try
            {
                var originalUsername = _currentUserService.Username;
                var originalUserId = _currentUserService.UserId;

                // Verify admin permission
                if (!_currentUserService.CanImpersonate())
                {
                    _logger.LogWarning("User {Username} attempted to impersonate without permission", originalUsername);
                    return new ImpersonateResponse(false, null, "You don't have permission to impersonate");
                }

                // Get user to impersonate
                var userToImpersonate = await _userRepository.GetByUsernameAsync(request.Username, true, cancellationToken);
                if (userToImpersonate == null)
                {
                    _logger.LogWarning("Attempt to impersonate non-existent user: {Username}", request.Username);
                    return new ImpersonateResponse(false, null, "User not found");
                }

                // Get all available roles
                var availableRoles = userToImpersonate.UserRoleMappings?
                    .Select(urm => urm.Role?.RoleName)
                    .Where(roleName => !string.IsNullOrEmpty(roleName))
                    .ToList() ?? new List<string>();

                if (!availableRoles.Any())
                {
                    _logger.LogWarning("Cannot impersonate user {Username} with no roles", request.Username);
                    return new ImpersonateResponse(false, null, "User has no roles assigned");
                }

                // Validate requested role
                string roleToImpersonate = null;
                if (!string.IsNullOrEmpty(request.Role))
                {
                    if (!_currentUserService.GetImpersonatableRoles().Contains(request.Role))
                    {
                        _logger.LogWarning("Attempt to impersonate invalid role {Role}", request.Role);
                        return new ImpersonateResponse(false, null, $"Cannot impersonate as {request.Role} role");
                    }

                    if (!availableRoles.Contains(request.Role))
                    {
                        _logger.LogWarning("User {Username} doesn't have role {Role}", request.Username, request.Role);
                        return new ImpersonateResponse(false, null, $"User doesn't have the '{request.Role}' role");
                    }

                    roleToImpersonate = request.Role;
                }

                // Create claims for impersonation
                var claims = new List<Claim>
                {
                    new Claim("original_user_id", originalUserId),
                    new Claim("original_username", originalUsername),
                    new Claim("is_impersonating", "true"),
                    new Claim("impersonation_role", roleToImpersonate ?? "all_roles"),
                    new Claim("original_roles", string.Join(",", availableRoles))
                };

                // Generate token
                string token;
                if (!string.IsNullOrEmpty(roleToImpersonate))
                {
                    // Specific role impersonation
                    token = _jwtService.GenerateRoleSpecificToken(
                        userToImpersonate.UserName,
                        roleToImpersonate,
                        availableRoles,
                        claims);
                }
                else
                {
                    // Full impersonation with all roles
                    token = _jwtService.GenerateToken(
                        userToImpersonate.UserName,
                        availableRoles,
                        claims);
                }

                _logger.LogInformation(
                    "User {OriginalUser} impersonated {ImpersonatedUser} with role {Role}",
                    originalUsername,
                    request.Username,
                    roleToImpersonate ?? "all roles");

                return new ImpersonateResponse(
                    true,
                    token,
                    null,
                    originalUsername,
                    request.Username,
                    roleToImpersonate,
                    availableRoles);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("Impersonation operation canceled: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during impersonation");
                return new ImpersonateResponse(false, null, "Impersonation failed due to system error");
            }
        }
    }
}