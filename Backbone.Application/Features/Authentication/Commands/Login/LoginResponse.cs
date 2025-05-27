//Backbone.Application/Features/Authentication/Commands/Login/LoginResponse.cs
namespace Backbone.Application.Features.Authentication.Commands.Login
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Error { get; set; }
        public bool RequiresRoleSelection { get; set; }

        public List<string> AvailableRoles { get; set; }
        public int UserId { get; set; }


        public LoginResponse() { }

        public LoginResponse(bool success, string token, string error,
                           bool requiresRoleSelection = false,
                           List<string> availableRoles = null,
                           int userId=0)
        {
            Success = success;
            Token = token;
            Error = error;
            RequiresRoleSelection = requiresRoleSelection;

            AvailableRoles = availableRoles;
            UserId = userId;

        }
    }
}