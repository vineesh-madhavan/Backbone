//Backbone.Application/Features/Authentication/Commands/SelectRole/SelectRoleResponse.cs

namespace Backbone.Application.Features.Authentication.Commands.SelectRole
{
    public class SelectRoleResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Error { get; set; }
        public string CurrentRole { get; set; }

        public SelectRoleResponse() { }

        public SelectRoleResponse(bool success, string token, string error, string currentRole = null)
        {
            Success = success;
            Token = token;
            Error = error;
            CurrentRole = currentRole;
        }
    }
}
