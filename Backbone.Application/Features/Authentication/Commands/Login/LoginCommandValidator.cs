//Backbone.Application/Features/Authentication/Commands/Login/LoginCommandValidator.cs
using FluentValidation;

namespace Backbone.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required");
                //.EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
                //.MinimumLength(8).WithMessage("Password must be at least 8 characters");
        }
    }
}
