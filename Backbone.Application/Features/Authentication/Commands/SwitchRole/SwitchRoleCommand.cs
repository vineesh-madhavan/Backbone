//Backbone Application/Features/Authentication/Commands/SwitchRole/SwitchRoleCommand.cs
using MediatR;

namespace Backbone.Application.Features.Authentication.Commands.SwitchRole
{
    public class SwitchRoleCommand : IRequest<SwitchRoleResponse>
    {
        public string NewRole { get; set; }

        public SwitchRoleCommand() { }

        public SwitchRoleCommand(string newRole)
        {
            NewRole = newRole;
        }
    }
}