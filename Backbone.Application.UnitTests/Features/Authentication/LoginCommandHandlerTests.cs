// tests/Backbone.Application.UnitTests/Features/Authentication/LoginCommandHandlerTests.cs
using Moq;
using FluentAssertions;
using Backbone.Application.Features.Authentication.Handlers;
using Backbone.Application.Features.Authentication.Commands.Login;
using Microsoft.Extensions.Logging;
using Backbone.Core.Interfaces;

namespace Backbone.Application.UnitTests.Features.Authentication
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock = new();
        private readonly LoginCommandHandler _sut;

        public LoginCommandHandlerTests()
        {
            _sut = new LoginCommandHandler(
                _jwtServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCredentials_ReturnsToken()
        {
            // Arrange
            var command = new LoginCommand("admin", "password");
            _jwtServiceMock
                .Setup(x => x.GenerateToken(It.IsAny<string>()))
                .Returns("fake_token");

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Token.Should().Be("fake_token");
            _jwtServiceMock.Verify(x => x.GenerateToken("admin"), Times.Once);
        }

        [Theory]
        [InlineData("wrong", "credentials")]
        [InlineData("admin", "wrongpassword")]
        public async Task Handle_InvalidCredentials_ReturnsFailure(string username, string password)
        {
            // Arrange
            var command = new LoginCommand(username, password);

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
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
    }
}