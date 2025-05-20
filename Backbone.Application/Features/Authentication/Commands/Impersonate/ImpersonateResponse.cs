//Backbone.Application/Features/Authentication/Commands/Impersonate/ImpersonateResponse.cs

namespace Backbone.Application.Features.Authentication.Commands.Impersonate
{
    public class ImpersonateResponse
    {
        public string Token { get; set; }
        public string OriginalUsername { get; set; }
        public string ImpersonatedUsername { get; set; }
        public string ImpersonatedRole { get; set; }
    }
}