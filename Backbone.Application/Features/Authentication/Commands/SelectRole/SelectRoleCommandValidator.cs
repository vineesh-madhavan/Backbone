//Backbone.Application/Features/Authentication/Commands/SelectRole/SelectRoleCommandValidator.cs
using FluentValidation;

namespace Backbone.Application.Features.Authentication.Commands.SelectRole
{
    public class SelectRoleCommandValidator : AbstractValidator<SelectRoleCommand>
    {
        public SelectRoleCommandValidator()
        {
            RuleFor(x => x.TempToken)
                .NotEmpty().WithMessage("Token is required");

            RuleFor(x => x.SelectedRole)
                .NotEmpty().WithMessage("Role selection is required");
        }
    }
}