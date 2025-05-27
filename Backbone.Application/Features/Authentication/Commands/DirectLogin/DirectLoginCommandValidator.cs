//Backbone.Application/Features/Authentication/Commands/DirectLogin/DirectLoginCommandValidator.cs
using FluentValidation;

namespace Backbone.Application.Features.Authentication.Commands.DirectLogin
{
    public class DirectLoginCommandValidator : AbstractValidator<DirectLoginCommand>
    {
        public DirectLoginCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required");
        }
    }
}
