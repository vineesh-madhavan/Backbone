//Backbone.Application/Features/Authentication/Commands/Impersonate/ImpersonateCommandValidator.cs

using FluentValidation;

namespace Backbone.Application.Features.Authentication.Commands.Impersonate
{
    public class ImpersonateCommandValidator : AbstractValidator<ImpersonateCommand>
    {
        public ImpersonateCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required");

            RuleFor(x => x.Role)
                .Must(role => string.IsNullOrEmpty(role) ||
                     new[] { "Admin", "Master", "Subscriber" }.Contains(role))
                .WithMessage("Invalid role specified for impersonation");
        }
    }
}