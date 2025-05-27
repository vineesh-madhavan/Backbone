//Backbone.Application/Features/Authentication/Commands/Impersonate/ImpersonateResponse.cs

namespace Backbone.Application.Features.Authentication.Commands.Impersonate
{
    public class ImpersonateResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Error { get; set; }
        public string OriginalUsername { get; set; }
        public string ImpersonatedUsername { get; set; }
        public string ImpersonatedRole { get; set; }
        public List<string> AvailableRoles { get; set; }

        public ImpersonateResponse() { }

        public ImpersonateResponse(bool success, string token, string error = null,
                                string originalUsername = null,
                                string impersonatedUsername = null,
                                string impersonatedRole = null,
                                List<string> availableRoles = null)
        {
            Success = success;
            Token = token;
            Error = error;
            OriginalUsername = originalUsername;
            ImpersonatedUsername = impersonatedUsername;
            ImpersonatedRole = impersonatedRole;
            AvailableRoles = availableRoles;
        }
    }
}