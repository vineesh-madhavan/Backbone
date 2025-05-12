// tests/Backbone.Application.UnitTests/Features/Authentication/LoginCommandHandlerTests.cs
// Backbone.Application.UnitTests/Features/Authentication/Handlers/LoginCommandHandlerTests.cs
using Backbone.Application.Features.Authentication.Commands.Login;
using Backbone.Application.Features.Authentication.Handlers;
using Backbone.Core.Entities;
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Backbone.Application.UnitTests.Features.Authentication.Handlers
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly LoginCommandHandler _sut;

        public LoginCommandHandlerTests()
        {
            _sut = new LoginCommandHandler(
                _jwtServiceMock.Object,
                _loggerMock.Object,
                _userRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCredentials_ReturnsToken()
        {
            // Arrange
            const string username = "admin";
            const string password = "password";
            var command = new LoginCommand(username, password);

            // Setup password hash verification
            _userRepositoryMock
                .Setup(x => x.ValidateCredentialsAsync(username, password, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _userRepositoryMock
                .Setup(x => x.GetByUsernameAsync(username, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User
                {
                    UserId = 1,
                    UserName = username,
                    UserRoleMappings = new List<UserRoleMapping>
                    {
                        new UserRoleMapping { Role = new UserRole { RoleName = "Admin" } }
                    }
                });

            _jwtServiceMock
                .Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns("fake_token");

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Token.Should().Be("fake_token");
            result.ErrorMessage.Should().BeNull();

            _userRepositoryMock.Verify(
                x => x.ValidateCredentialsAsync(username, password, It.IsAny<CancellationToken>()),
                Times.Once);

            _jwtServiceMock.Verify(
                x => x.GenerateToken(username, It.Is<IEnumerable<string>>(roles => roles.Contains("Admin"))),
                Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidCredentials_ReturnsFailure()
        {
            // Arrange
            var command = new LoginCommand("invalid", "credentials");

            _userRepositoryMock
                .Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Token.Should().BeNull();
            result.ErrorMessage.Should().NotBeNullOrEmpty();

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed login attempt")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotFoundAfterValidation_ReturnsFailure()
        {
            // Arrange
            var command = new LoginCommand("admin", "password");

            _userRepositoryMock
                .Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _userRepositoryMock
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Token.Should().BeNull();
            result.ErrorMessage.Should().NotBeNullOrEmpty();

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User not found after credential validation")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}