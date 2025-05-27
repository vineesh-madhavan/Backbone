//Backbone Application/Features/Authentication/Commands/SelectRole/SelectRoleCommandHandler.cs
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backbone.Application.Features.Authentication.Commands.SelectRole
{
    public class SelectRoleCommandHandler : IRequestHandler<SelectRoleCommand, SelectRoleResponse>
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger<SelectRoleCommandHandler> _logger;
        private readonly IUserRepository _userRepository;

        public SelectRoleCommandHandler(
            IJwtService jwtService,
            ILogger<SelectRoleCommandHandler> logger,
            IUserRepository userRepository)
        {
            _jwtService = jwtService;
            _logger = logger;
            _userRepository = userRepository;
        }

        public async Task<SelectRoleResponse> Handle(SelectRoleCommand request, CancellationToken cancellationToken)
        {
            using var _ = _logger.BeginScope(new { RequestId = Guid.NewGuid() });

            try
            {
                _logger.LogInformation("Role selection attempt initiated");

                // Validate temp token
                var principal = _jwtService.ValidateToken(request.TempToken);
                if (principal == null)
                {
                    _logger.LogWarning("Invalid token provided for role selection");
                    return new SelectRoleResponse(false, null, "Invalid token");
                }

                var username = principal.FindFirstValue(ClaimTypes.Name);
                var originalRoles = _jwtService.GetOriginalRolesFromToken(request.TempToken).ToList();

                if (!originalRoles.Contains(request.SelectedRole))
                {
                    _logger.LogWarning("User {Username} attempted to select invalid role {Role}",
                        username, request.SelectedRole);
                    return new SelectRoleResponse(false, null, "Invalid role selection");
                }

                // Get user to verify
                var user = await _userRepository.GetByUsernameAsync(username, false, cancellationToken);
                if (user == null)
                {
                    _logger.LogError("User not found for role selection: {Username}", username);
                    return new SelectRoleResponse(false, null, "System error");
                }

                // Generate final token
                _logger.LogDebug("Generating final JWT token for {Username} with role {Role}",
                    username, request.SelectedRole);
                var token = _jwtService.GenerateRoleSpecificToken(username, request.SelectedRole, originalRoles);

                _logger.LogInformation("Successful role selection for {Username}", username);
                return new SelectRoleResponse(true, token, null, request.SelectedRole);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("Role selection operation canceled: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during role selection");
                throw new AuthenticationException("Role selection failed due to system error");
            }
        }
    }
}