//Backbone.Application/Features/Authentication/Commands/SwitchRole/SwitchRoleResponse.cs
namespace Backbone.Application.Features.Authentication.Commands.SwitchRole
{
    public class SwitchRoleResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Error { get; set; }
        public string PreviousRole { get; set; }
        public string NewRole { get; set; }
        public List<string> AvailableRoles { get; set; }

        public SwitchRoleResponse() { }

        public SwitchRoleResponse(bool success, string token, string error = null,
                                string previousRole = null,
                                string newRole = null,
                                List<string> availableRoles = null)
        {
            Success = success;
            Token = token;
            Error = error;
            PreviousRole = previousRole;
            NewRole = newRole;
            AvailableRoles = availableRoles;
        }
    }
}