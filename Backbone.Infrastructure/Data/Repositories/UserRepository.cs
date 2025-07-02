  //Backbone.Infrastructure/Data/Repositories/UserRepository.cs
using Backbone.Core.Entities;
using Backbone.Core.Interfaces.Data.Repositories;
using Backbone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Backbone.Core.Specifications;
using Backbone.Core.Interfaces;
using Core.Secutiy;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Backbone.Infrastructure.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { UserId = userId, Method = nameof(GetByIdAsync) });
            _logger.LogDebug("Getting user by ID");

            try
            {
                var query = _context.Users.AsQueryable();
                var user = await query.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                }
                else
                {
                    _logger.LogDebug("Successfully retrieved user with ID {UserId}", userId);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
                throw;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string userName, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { UserName = userName, Method = nameof(GetUserByUsernameAsync) });
            _logger.LogDebug("Getting user by username");

            try
            {
                var query = _context.Users.AsQueryable();
                var user = await query.FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with username {UserName} not found", userName);
                }
                else
                {
                    _logger.LogDebug("Successfully retrieved user with username {UserName}", userName);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username {UserName}", userName);
                throw;
            }
        }

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { Method = nameof(GetAllAsync) });
            _logger.LogDebug("Getting all users");

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var users = await _context.Users
                    .Where(u => !u.IsDeleted)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation("Retrieved {Count} users in {ElapsedMilliseconds}ms",
                    users.Count, stopwatch.ElapsedMilliseconds);

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<User?> GetUserWithRolesAsync(int userId, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { UserId = userId, Method = nameof(GetUserWithRolesAsync) });
            _logger.LogDebug("Getting user with roles");

            try
            {
                var query = _context.Users.AsQueryable();
                query = query
                    .Include(u => u.UserRoleMappings)
                        .ThenInclude(urm => urm.Role);

                var user = await query.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found when loading roles", userId);
                }
                else
                {
                    _logger.LogDebug("Successfully retrieved user with ID {UserId} and {RoleCount} roles",
                        userId, user.UserRoleMappings?.Count ?? 0);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with roles for ID {UserId}", userId);
                throw;
            }
        }  

        public async Task<User?> GetUserWithDetailsAsync(int userId, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { UserId = userId, Method = nameof(GetUserWithDetailsAsync) });
            _logger.LogDebug("Getting user with details");

            try
            {
                var query = _context.Users.AsQueryable();
                query = query
                    .Include(u => u.Status)
                    .Include(u => u.UserDetails)
                    .Include(u => u.UserAddresses)
                    .Include(u => u.UserRoleMappings)
                        .ThenInclude(urm => urm.Role);

                var user = await query.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found when loading details", userId);
                }
                else
                {
                    _logger.LogDebug("Successfully retrieved user with ID {UserId} and full details", userId);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with details for ID {UserId}", userId);
                throw;
            }
        }

        public async Task<User?> GetByUsernameAsync(string username, bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { Username = username, IncludeDetails = includeDetails, Method = nameof(GetByUsernameAsync) });
            _logger.LogDebug("Getting user by username with details flag: {IncludeDetails}", includeDetails);

            try
            {
                var query = _context.Users
                    .Where(u => u.UserName == username);

                if (includeDetails)
                {
                    query = query
                        .Include(u => u.Status)
                        .Include(u => u.UserDetails)
                        .Include(u => u.UserAddresses)
                        .Include(u => u.UserRoleMappings)
                            .ThenInclude(urm => urm.Role);
                    _logger.LogDebug("Added detailed includes for user query");
                }

                var user = await query.FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with username {Username} not found", username);
                }
                else
                {
                    _logger.LogDebug("Successfully retrieved user with username {Username}", username);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username {Username}", username);
                throw;
            }
        }

        public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { Username = username, Method = nameof(IsUsernameTakenAsync) });
            _logger.LogDebug("Checking if username is taken");

            try
            {
                var isTaken = await _context.Users
                    .AnyAsync(u => u.UserName == username, cancellationToken);

                _logger.LogDebug("Username {Username} is {Status}", username, isTaken ? "taken" : "available");
                return isTaken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if username {Username} is taken", username);
                throw;
            }
        }
        public async Task<bool> IsActive(string username, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { Username = username, Method = nameof(IsUsernameTakenAsync) });
            _logger.LogDebug("Checking if user is active");

            try
            {
                var isActive = await _context.Users
                    .AnyAsync(u => u.UserName == username && !u.IsDeleted, cancellationToken); // Add logic for user status

                _logger.LogDebug("Username {Username} is {Status}", username, isActive ? "active" : "inactive");
                return isActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if username {Username} is taken", username);
                throw;
            }
        }

        public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { Username = user.UserName, Method = nameof(AddAsync) });
            _logger.LogDebug("Adding new user");

            try
            {
                await _context.Users.AddAsync(user, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added new user with ID {UserId} and username {Username}",
                    user.UserId, user.UserName);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding new user with username {Username}", user.UserName);
                throw;
            }
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { UserId = user.UserId, Method = nameof(UpdateAsync) });
            _logger.LogDebug("Updating user");

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated user with ID {UserId}", user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", user.UserId);
                throw;
            }
        }

        public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { UserId = user.UserId, Method = nameof(DeleteAsync) });
            _logger.LogDebug("Soft deleting user");

            try
            {
                user.IsDeleted = true;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully soft deleted user with ID {UserId}", user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting user with ID {UserId}", user.UserId);
                throw;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { Username = username, Method = nameof(ValidateCredentialsAsync) });
            _logger.LogDebug("Validating credentials");

            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with username {Username} not found during credential validation", username);
                    // Perform dummy hash to prevent timing attacks
                    using (var hmac = new HMACSHA512())
                    {
                        hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                    }
                    return false;
                }

                var isValid = user.PasswordHash.VerifyPasswordHash(user.PasswordSalt, password, _logger);
                _logger.LogDebug("Credential validation result for {Username}: {IsValid}", username, isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for username {Username}", username);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new { UserId = id, Method = nameof(ExistsAsync) });
            _logger.LogDebug("Checking if user exists");

            try
            {
                var exists = await _context.Users
                    .AnyAsync(u => u.UserId == id && !u.IsDeleted, cancellationToken);

                _logger.LogDebug("User with ID {UserId} exists: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user with ID {UserId} exists", id);
                throw;
            }
        }

        public async Task<User?> GetFirstOrDefaultAsync(
            ISpecification<User> spec,
            CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new
            {
                Specification = spec.GetType().Name,
                Method = nameof(GetFirstOrDefaultAsync)
            });
            _logger.LogDebug("Getting first or default user using specification");

            try
            {
                var user = await ApplySpecification(spec)
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    _logger.LogDebug("No user found matching specification");
                }
                else
                {
                    _logger.LogDebug("Found user with ID {UserId} matching specification", user.UserId);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user using specification");
                throw;
            }
        }

        public async Task<IReadOnlyList<User>> ListAsync(
            ISpecification<User> spec,
            CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new
            {
                Specification = spec.GetType().Name,
                Method = nameof(ListAsync)
            });
            _logger.LogDebug("Listing users using specification");

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var users = await ApplySpecification(spec)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation("Retrieved {Count} users using specification in {ElapsedMilliseconds}ms",
                    users.Count, stopwatch.ElapsedMilliseconds);

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing users using specification");
                throw;
            }
        }

        public async Task<int> CountAsync(
            ISpecification<User> spec,
            CancellationToken cancellationToken = default)
        {
            using var _ = _logger.BeginScope(new
            {
                Specification = spec.GetType().Name,
                Method = nameof(CountAsync)
            });
            _logger.LogDebug("Counting users using specification");

            try
            {
                var count = await ApplySpecification(spec)
                    .CountAsync(cancellationToken);

                _logger.LogDebug("Counted {Count} users matching specification", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting users using specification");
                throw;
            }
        }

        private IQueryable<User> ApplySpecification(ISpecification<User> spec)
        {
            return SpecificationEvaluator<User>.GetQuery(
                _context.Users.AsQueryable(),
                spec,
                _logger);
        }
    }
}
