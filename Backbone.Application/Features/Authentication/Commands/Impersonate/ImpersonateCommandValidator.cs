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
        }
    }
}