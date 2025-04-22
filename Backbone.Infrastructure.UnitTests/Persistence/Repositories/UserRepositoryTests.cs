//Backbone.Infrastructure.UnitTests/Persistence/Repositories/UserRepositoryTests.cs
using Backbone.Core.Entities;
using Backbone.Infrastructure.Data;
using Backbone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Backbone.Infrastructure.UnitTests.Persistence.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly EfRepository<User> _repository;

        public UserRepositoryTests()
        {
            // Use in-memory database for tests
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _repository = new EfRepository<User>(_dbContext);
        }

        [Fact]
        public async Task AddAsync_AddsUserToDatabase()
        {
            // Arrange
            var user = new User { Username = "testuser" };

            // Act
            await _repository.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Assert
            var dbUser = await _dbContext.Users.FirstOrDefaultAsync();
            Assert.NotNull(dbUser);
            Assert.Equal("testuser", dbUser.Username);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}