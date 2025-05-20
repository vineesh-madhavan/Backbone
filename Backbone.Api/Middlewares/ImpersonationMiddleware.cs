// Backbone.Api/Middleware/ImpersonationMiddleware.cs
using Backbone.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Backbone.Api.Middleware
{
    public class ImpersonationMiddleware
    {
        private readonly RequestDelegate _next;

        public ImpersonationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ICurrentUserService currentUserService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var isImpersonating = context.User.Claims
                    .FirstOrDefault(c => c.Type == "is_impersonating")?.Value == "true";

                if (isImpersonating)
                {
                    currentUserService.IsImpersonating = true;
                    currentUserService.OriginalUsername = context.User.Claims
                        .FirstOrDefault(c => c.Type == "original_username")?.Value;
                    currentUserService.ImpersonatedRole = context.User.Claims
                        .FirstOrDefault(c => c.Type == "impersonation_role")?.Value;
                }
            }

            await _next(context);
        }
    }
}