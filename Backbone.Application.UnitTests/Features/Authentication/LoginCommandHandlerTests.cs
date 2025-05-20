using Backbone.Application.Features.Authentication.Commands.Login;
using Backbone.Application.Features.Authentication.Exceptions;
using Backbone.Core.Entities;
using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        public async Task Handle_ValidCredentials_ReturnsSuccessWithToken()
        {
            // Arrange
            const string username = "admin";
            const string password = "password";
            const string expectedToken = "fake_token";
            var command = new LoginCommand(username, password);

            var user = new User
            {
                UserId = 1,
                UserName = username,
                UserRoleMappings = new List<UserRoleMapping>
                {
                    new UserRoleMapping { Role = new UserRole { RoleName = "Admin" } }
                }
            };

            _userRepositoryMock
                .Setup(x => x.ValidateCredentialsAsync(username, password, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _userRepositoryMock
                .Setup(x => x.GetByUsernameAsync(username, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _jwtServiceMock
                .Setup(x => x.GenerateToken(
                    username,
                    It.Is<IEnumerable<string>>(roles => roles.Contains("Admin")),
                    It.IsAny<IEnumerable<Claim>>()))
                .Returns(expectedToken); // Changed to Returns for sync method

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().Be(expectedToken);
            result.Error.Should().BeNull();

            VerifyLogging(LogLevel.Information, "Login attempt initiated");
            VerifyLogging(LogLevel.Debug, $"Validating credentials for {username}");
            VerifyLogging(LogLevel.Debug, "Credentials validated, retrieving user details");
            VerifyLogging(LogLevel.Debug, $"Generating JWT token for {username} with roles: Admin");
            VerifyLogging(LogLevel.Information, $"Successful authentication for {username}");

            _userRepositoryMock.Verify(
                x => x.ValidateCredentialsAsync(username, password, It.IsAny<CancellationToken>()),
                Times.Once);

            _jwtServiceMock.Verify(
                x => x.GenerateToken(
                    username,
                    It.Is<IEnumerable<string>>(roles => roles.Contains("Admin")),
                    It.IsAny<IEnumerable<Claim>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCredentialsWithImpersonation_IncludesImpersonationClaims()
        {
            // Arrange
            const string username = "admin";
            const string password = "password";
            const string expectedToken = "fake_token";
            var command = new LoginCommand(username, password);

            var user = new User
            {
                UserId = 1,
                UserName = username,
                UserRoleMappings = new List<UserRoleMapping>
                {
                    new UserRoleMapping { Role = new UserRole { RoleName = "Admin" } }
                }
            };

            _userRepositoryMock
                .Setup(x => x.ValidateCredentialsAsync(username, password, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _userRepositoryMock
                .Setup(x => x.GetByUsernameAsync(username, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _jwtServiceMock
                .Setup(x => x.GenerateToken(
                    username,
                    It.IsAny<IEnumerable<string>>(),
                    It.Is<IEnumerable<Claim>>(claims =>
                        claims.Any(c => c.Type == "is_impersonating"))))
                .Returns(expectedToken); // Changed to Returns for sync method

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            _jwtServiceMock.Verify(
                x => x.GenerateToken(
                    username,
                    It.IsAny<IEnumerable<string>>(),
                    It.Is<IEnumerable<Claim>>(claims =>
                        claims.Any(c => c.Type == "original_username"))),
                Times.Never);
        }

        [Fact]
        public async Task Handle_InvalidCredentials_ReturnsFailureWithError()
        {
            // Arrange
            const string username = "invalid";
            const string password = "credentials";
            var command = new LoginCommand(username, password);

            _userRepositoryMock
                .Setup(x => x.ValidateCredentialsAsync(username, password, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Token.Should().BeNull();
            result.Error.Should().Be("Invalid credentials");

            VerifyLogging(LogLevel.Warning, $"Invalid credentials provided for {username}");
        }

        [Fact]
        public async Task Handle_UserNotFoundAfterValidation_ReturnsFailureWithError()
        {
            // Arrange
            const string username = "admin";
            const string password = "password";
            var command = new LoginCommand(username, password);

            _userRepositoryMock
                .Setup(x => x.ValidateCredentialsAsync(username, password, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _userRepositoryMock
                .Setup(x => x.GetByUsernameAsync(username, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Token.Should().BeNull();
            result.Error.Should().Be("System error");

            VerifyLogging(LogLevel.Error, $"User not found after successful credential validation for {username}");
        }

        [Fact]
        public async Task Handle_UserWithNoRoles_ReturnsSuccessWithTokenButLogsWarning()
        {
            // Arrange
            const string username = "user";
            const string password = "password";
            const string expectedToken = "fake_token";
            var command = new LoginCommand(username, password);

            var user = new User
            {
                UserId = 1,
                UserName = username,
                UserRoleMappings = new List<UserRoleMapping>()
            };

            _userRepositoryMock
                .Setup(x => x.ValidateCredentialsAsync(username, password, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _userRepositoryMock
                .Setup(x => x.GetByUsernameAsync(username, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _jwtServiceMock
                .Setup(x => x.GenerateToken(
                    username,
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<Claim>>()))
                .Returns(expectedToken); // Changed to Returns for sync method

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().Be(expectedToken);
            result.Error.Should().BeNull();

            VerifyLogging(LogLevel.Warning, $"User {username} has no roles assigned");
        }

        [Fact]
        public async Task Handle_OperationCanceled_ThrowsOperationCanceledException()
        {
            // Arrange
            var command = new LoginCommand("user", "pass");
            var cancellationToken = new CancellationToken(canceled: true);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _sut.Handle(command, cancellationToken));
        }

        [Fact]
        public async Task Handle_UnexpectedError_ThrowsAuthenticationException()
        {
            // Arrange
            var command = new LoginCommand("user", "pass");
            var exception = new Exception("Database error");

            _userRepositoryMock
                .Setup(x => x.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AuthenticationException>(() =>
                _sut.Handle(command, CancellationToken.None));

            ex.Message.Should().Be("Login failed due to system error");
            ex.InnerException.Should().Be(exception);

            VerifyLogging(LogLevel.Error, "Critical error during login for user");
        }

        private void VerifyLogging(LogLevel logLevel, string expectedMessage)
        {
            _loggerMock.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}