//Backbone.Infrastructure.UnitTests/Persistence/Repositories/UserRepositoryTests.cs
using Backbone.Core.Entities;
using Backbone.Core.Interfaces;
using Backbone.Infrastructure.Data;
using Backbone.Infrastructure.Persistence;
using Backbone.Infrastructure.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Backbone.Infrastructure.UnitTests.Persistence.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly EfRepository<User> _repository;
        private readonly MockCurrentUserService _currentUserService;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _repository = new EfRepository<User>(_dbContext);
            _currentUserService = new MockCurrentUserService();
        }

        [Fact]
        public async Task AddAsync_AddsUserToDatabase()
        {
            var user = new User { UserName = "testuser" };

            await _repository.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            var dbUser = await _dbContext.Users.FirstOrDefaultAsync();
            Assert.NotNull(dbUser);
            Assert.Equal("testuser", dbUser.UserName);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsUser_WhenUserExists()
        {
            var user = new User { UserName = "existinguser" };
            await _repository.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(user.UserId);

            Assert.NotNull(result);
            Assert.Equal("existinguser", result.UserName);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            _dbContext.Users.Add(new User { UserName = "user1" });
            _dbContext.Users.Add(new User { UserName = "user2" });
            await _dbContext.SaveChangesAsync();

            var users = await _repository.GetAllAsync();

            Assert.Equal(2, users.Count());
        }

        [Fact]
        public async Task DeleteAsync_RemovesUserFromDatabase()
        {
            var user = new User { UserName = "tobedeleted" };
            await _repository.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            await _repository.DeleteAsync(user);
            await _dbContext.SaveChangesAsync();

            var dbUser = await _repository.GetByIdAsync(user.UserId);
            Assert.Null(dbUser);
        }

        [Fact]
        public async Task UpdateAsync_ModifiesExistingUser()
        {
            var user = new User { UserName = "original" };
            await _repository.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            user.UserName = "modified";
            await _repository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            var dbUser = await _repository.GetByIdAsync(user.UserId);
            Assert.Equal("modified", dbUser.UserName);
        }

        [Fact]
        public async Task GetUsersByRole_ReturnsOnlyUsersWithSpecifiedRole()
        {
            var adminRole = new UserRole { RoleName = "Admin" };
            var subscriberRole = new UserRole { RoleName = "Subscriber" };

            var adminUser = new User
            {
                UserName = "admin",
                UserRoleMappings = new List<UserRoleMapping> { new UserRoleMapping { Role = adminRole } }
            };

            var regularUser = new User
            {
                UserName = "subscriber",
                UserRoleMappings = new List<UserRoleMapping> { new UserRoleMapping { Role = subscriberRole } }
            };

            _dbContext.Users.Add(adminUser);
            _dbContext.Users.Add(regularUser);
            await _dbContext.SaveChangesAsync();

            var adminUsers = await _dbContext.Users
                .Where(u => u.UserRoleMappings.Any(urm => urm.Role.RoleName == "Admin"))
                .ToListAsync();

            Assert.Single(adminUsers);
            Assert.Equal("admin", adminUsers.First().UserName);
        }

        [Fact]
        public async Task AddUserWithRoles_CorrectlyStoresRoleMappings()
        {
            var adminRole = new UserRole { RoleName = "Admin" };
            _dbContext.UserRoles.Add(adminRole);
            await _dbContext.SaveChangesAsync();

            var user = new User
            {
                UserName = "newadmin",
                UserRoleMappings = new List<UserRoleMapping> { new UserRoleMapping { RoleId = adminRole.RoleId } }
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var dbUser = await _dbContext.Users
                .Include(u => u.UserRoleMappings)
                .ThenInclude(urm => urm.Role)
                .FirstOrDefaultAsync(u => u.UserName == "newadmin");

            Assert.NotNull(dbUser);
            Assert.Single(dbUser.UserRoleMappings);
            Assert.Equal("Admin", dbUser.UserRoleMappings.First().Role.RoleName);
        }

        [Fact]
        public async Task UpdateUserRoles_CorrectlyModifiesRoleAssignments()
        {
            var adminRole = new UserRole { RoleName = "Admin" };
            var subscriberRole = new UserRole { RoleName = "Subscriber" };
            _dbContext.UserRoles.AddRange(adminRole, subscriberRole);

            var user = new User
            {
                UserName = "testuser",
                UserRoleMappings = new List<UserRoleMapping> { new UserRoleMapping { Role = subscriberRole } }
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            user.UserRoleMappings.Add(new UserRoleMapping { Role = adminRole });
            await _repository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            var updatedUser = await _dbContext.Users
                .Include(u => u.UserRoleMappings)
                .ThenInclude(urm => urm.Role)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            Assert.Equal(2, updatedUser.UserRoleMappings.Count);
            Assert.Contains(updatedUser.UserRoleMappings, urm => urm.Role.RoleName == "Admin");
            Assert.Contains(updatedUser.UserRoleMappings, urm => urm.Role.RoleName == "Subscriber");
        }

        [Fact]
        public async Task GetUsersWithAdminRole_UsingCurrentUserService()
        {
            _currentUserService.Roles = new List<string> { "Admin" };

            var adminRole = new UserRole { RoleName = "Admin" };
            var subscriberRole = new UserRole { RoleName = "Subscriber" };
            _dbContext.UserRoles.AddRange(adminRole, subscriberRole);

            var users = new List<User>
            {
                new User {
                    UserName = "admin1",
                    UserRoleMappings = new List<UserRoleMapping> { new UserRoleMapping { Role = adminRole } }
                },
                new User {
                    UserName = "admin2",
                    UserRoleMappings = new List<UserRoleMapping> { new UserRoleMapping { Role = adminRole } }
                },
                new User {
                    UserName = "subscriber1",
                    UserRoleMappings = new List<UserRoleMapping> { new UserRoleMapping { Role = subscriberRole } }
                }
            };

            _dbContext.Users.AddRange(users);
            await _dbContext.SaveChangesAsync();

            var adminUsers = _currentUserService.IsAdmin()
                ? await _dbContext.Users
                    .Where(u => u.UserRoleMappings.Any(urm => urm.Role.RoleName == "Admin"))
                    .ToListAsync()
                : new List<User>();

            Assert.Equal(2, adminUsers.Count);
            Assert.DoesNotContain(adminUsers, u => u.UserName == "subscriber1");
        }

        [Fact]
        public async Task MasterRole_HasElevatedPrivileges()
        {
            _currentUserService.Roles = new List<string> { "Master" };

            var masterRole = new UserRole { RoleName = "Master" };
            var subscriberRole = new UserRole { RoleName = "Subscriber" };
            _dbContext.UserRoles.AddRange(masterRole, subscriberRole);

            var users = new List<User>
            {
                new User {
                    UserName = "master1",
                    UserRoleMappings = new List<UserRoleMapping> { new UserRoleMapping { Role = masterRole } }
                },
                new User {
                    UserName = "subscriber1",
                    UserRoleMappings = new List<UserRoleMapping> { new UserRoleMapping { Role = subscriberRole } }
                }
            };

            _dbContext.Users.AddRange(users);
            await _dbContext.SaveChangesAsync();

            var accessibleUsers = _currentUserService.IsInAnyRole("Admin", "Master")
                ? await _dbContext.Users.ToListAsync()
                : new List<User>();

            Assert.Equal(2, accessibleUsers.Count);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}