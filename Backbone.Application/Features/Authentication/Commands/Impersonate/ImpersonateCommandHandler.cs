//Backbone.Application/Features/Authentication/Commands/Impersonate/ImpersonateCommandHandler.cs

using Backbone.Core.Entities;
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
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
            var originalUsername = _currentUserService.Username;

            // Verify admin permission
            if (!_currentUserService.IsInRole("Admin"))
            {
                _logger.LogWarning("User {Username} attempted to impersonate without permission", originalUsername);
                throw new AuthenticationException("You don't have permission to impersonate");
            }

            // Get user to impersonate
            var userToImpersonate = await _userRepository.GetByUsernameAsync(request.Username, true, cancellationToken);
            if (userToImpersonate == null)
            {
                _logger.LogWarning("Attempt to impersonate non-existent user: {Username}", request.Username);
                throw new AuthenticationException("User not found");
            }

            // Get all available roles
            var availableRoles = userToImpersonate.UserRoleMappings?
                .Select(urm => urm.Role?.RoleName)
                .Where(roleName => !string.IsNullOrEmpty(roleName))
                .ToList() ?? new List<string>();

            // Handle role selection
            List<string> rolesToAssign;
            if (!string.IsNullOrEmpty(request.Role))
            {
                // Verify requested role exists
                if (!availableRoles.Contains(request.Role))
                {
                    throw new AuthenticationException($"User doesn't have the '{request.Role}' role");
                }
                rolesToAssign = new List<string> { request.Role };
            }
            else
            {
                // Use all roles if none specified
                rolesToAssign = availableRoles;
            }

            // Create claims
            var claims = new List<Claim>
            {
                new Claim("original_username", originalUsername),
                new Claim("is_impersonating", "true"),
                new Claim("impersonation_role", request.Role ?? "all_roles")
            };

            // Generate token
            var token = _jwtService.GenerateToken(request.Username, rolesToAssign, claims);

            _logger.LogInformation(
                "User {OriginalUser} impersonated {ImpersonatedUser} with role {Role}",
                originalUsername,
                request.Username,
                request.Role ?? "all roles");

            return new ImpersonateResponse
            {
                Token = token,
                OriginalUsername = originalUsername,
                ImpersonatedUsername = request.Username,
                ImpersonatedRole = request.Role ?? "all roles"
            };
        }
    }
}