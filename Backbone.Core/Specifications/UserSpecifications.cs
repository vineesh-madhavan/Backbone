//Backbone.Core/Specifications/UserSpecifications.cs
using Backbone.Core.Entities;
using Microsoft.Extensions.Logging;
using System;

namespace Backbone.Core.Specifications
{
    public class UserWithDetailsSpecification : BaseSpecification<User>
    {
        private readonly ILogger<UserWithDetailsSpecification> _logger;

        public UserWithDetailsSpecification(int userId, ILogger<UserWithDetailsSpecification> logger = null)
            : base(u => u.UserId == userId)
        {
            _logger = logger;

            using var _ = _logger?.BeginScope(new { UserId = userId, Specification = nameof(UserWithDetailsSpecification) });
            _logger?.LogDebug("Creating UserWithDetailsSpecification for UserId: {UserId}", userId);

            try
            {
                AddInclude(u => u.UserDetails);
                AddInclude(u => u.UserAddresses);
                AddInclude(u => u.UserRoleMappings);
                AddInclude("UserRoleMappings.Role");

                _logger?.LogDebug("Added includes for UserDetails, UserAddresses, UserRoleMappings, and Roles");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating UserWithDetailsSpecification for UserId: {UserId}", userId);
                throw;
            }
        }

        public UserWithDetailsSpecification(string username, ILogger<UserWithDetailsSpecification> logger = null)
            : base(u => u.UserName == username)
        {
            _logger = logger;

            using var _ = _logger?.BeginScope(new { Username = username, Specification = nameof(UserWithDetailsSpecification) });
            _logger?.LogDebug("Creating UserWithDetailsSpecification for Username: {Username}", username);

            try
            {
                AddInclude(u => u.UserDetails);
                AddInclude(u => u.Status);

                _logger?.LogDebug("Added includes for UserDetails and Status");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating UserWithDetailsSpecification for Username: {Username}", username);
                throw;
            }
        }
    }

    public class ActiveUsersSpecification : BaseSpecification<User>
    {
        private readonly ILogger<ActiveUsersSpecification> _logger;

        public ActiveUsersSpecification(int statusId, ILogger<ActiveUsersSpecification> logger = null)
            : base(u => u.StatusId == statusId)
        {
            _logger = logger;

            using var _ = _logger?.BeginScope(new { StatusId = statusId, Specification = nameof(ActiveUsersSpecification) });
            _logger?.LogDebug("Creating ActiveUsersSpecification for StatusId: {StatusId}", statusId);

            try
            {
                ApplyOrderBy(u => u.UserName);
                _logger?.LogDebug("Applied default ordering by UserName");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating ActiveUsersSpecification for StatusId: {StatusId}", statusId);
                throw;
            }
        }

        public ActiveUsersSpecification(int skip, int take, int statusId, ILogger<ActiveUsersSpecification> logger = null)
            : base(u => u.StatusId == statusId)
        {
            _logger = logger;

            using var _ = _logger?.BeginScope(new
            {
                StatusId = statusId,
                Skip = skip,
                Take = take,
                Specification = nameof(ActiveUsersSpecification)
            });

            _logger?.LogDebug(
                "Creating paginated ActiveUsersSpecification (StatusId: {StatusId}, Skip: {Skip}, Take: {Take})",
                statusId, skip, take);

            try
            {
                ApplyOrderBy(u => u.UserName);
                ApplyPaging(skip, take);

                _logger?.LogDebug("Applied ordering by UserName and paging (Skip: {Skip}, Take: {Take})", skip, take);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error creating paginated ActiveUsersSpecification (StatusId: {StatusId}, Skip: {Skip}, Take: {Take})",
                    statusId, skip, take);
                throw;
            }
        }
    }
}