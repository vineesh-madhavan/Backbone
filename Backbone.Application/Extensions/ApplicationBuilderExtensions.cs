// Backbone.Application/Extensions/ApplicationBuilderExtensions.cs
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Backbone.Application.Extensions
{
    public static class ApplicationBuilderExtensions  // Must be static
    {
        public static IApplicationBuilder UseCustomRequestLogging(this IApplicationBuilder app)
        {
            return app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                };
            });
        }
    }
}