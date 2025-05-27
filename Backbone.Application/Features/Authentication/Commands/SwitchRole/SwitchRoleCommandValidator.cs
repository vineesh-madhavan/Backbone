//Backbone.Application/Features/Authentication/Commands/SwitchRole/SwitchRoleCommandValidator.cs
using FluentValidation;

namespace Backbone.Application.Features.Authentication.Commands.SwitchRole
{
    public class SwitchRoleCommandValidator : AbstractValidator<SwitchRoleCommand>
    {
        public SwitchRoleCommandValidator()
        {
            RuleFor(x => x.NewRole)
                .NotEmpty().WithMessage("Role is required");
        }
    }
}