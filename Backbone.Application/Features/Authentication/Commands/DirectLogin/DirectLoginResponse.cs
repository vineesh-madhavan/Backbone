//Backbone.Application/Features/Authentication/Commands/DirectLogin/DirectLoginResponse.cs
namespace Backbone.Application.Features.Authentication.Commands.DirectLogin
{
    public class DirectLoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Error { get; set; }
        public string CurrentRole { get; set; }
        public List<string> AvailableRoles { get; set; }

        public DirectLoginResponse() { }

        public DirectLoginResponse(bool success, string token, string error,
                                 string currentRole = null,
                                 List<string> availableRoles = null)
        {
            Success = success;
            Token = token;
            Error = error;
            CurrentRole = currentRole;
            AvailableRoles = availableRoles;
        }
    }
}