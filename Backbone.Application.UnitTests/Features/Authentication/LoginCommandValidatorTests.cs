using Backbone.Application.Features.Authentication.Commands.Login;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Backbone.Application.UnitTests.Features.Authentication
{
    public class LoginCommandValidatorTests
    {
        private readonly LoginCommandValidator _validator = new();

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new LoginCommand("admin", "password123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(null)] // null username
        [InlineData("")]  // empty username
        [InlineData("   ")] // whitespace username
        public void Validate_InvalidUsername_ShouldFail(string username)
        {
            // Arrange
            var command = new LoginCommand(username, "validpassword");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Username is required");
        }

        [Theory]
        [InlineData(null)] // null password
        [InlineData("")]  // empty password
        [InlineData("   ")] // whitespace password
        public void Validate_InvalidPassword_ShouldFail(string password)
        {
            // Arrange
            var command = new LoginCommand("admin", password);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password is required");
        }

        [Fact]
        public void Validate_EmptyCommand_ShouldFailBothValidations()
        {
            // Arrange
            var command = new LoginCommand("", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.Errors.Count.Should().Be(2);
            result.ShouldHaveValidationErrorFor(x => x.Username);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }
    }
}