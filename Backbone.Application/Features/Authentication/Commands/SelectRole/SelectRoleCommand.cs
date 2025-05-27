//Backbone.Application/Features/Authentication/Commands/SelectRole/SelectRoleCommand.cs

using MediatR;

namespace Backbone.Application.Features.Authentication.Commands.SelectRole
{
    public class SelectRoleCommand : IRequest<SelectRoleResponse>
    {
        public string TempToken { get; set; }
        public string SelectedRole { get; set; }

        public SelectRoleCommand() { }

        public SelectRoleCommand(string tempToken, string selectedRole)
        {
            TempToken = tempToken;
            SelectedRole = selectedRole;
        }
    }
}
