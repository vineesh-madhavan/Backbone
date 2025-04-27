// Backbone.Infrastructure/Logging/SerilogExtensions.cs
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Backbone.Infrastructure.Logging
{
    public static class SerilogExtensions  // Must be static
    {
        public static IHostBuilder UseCustomSerilog(this IHostBuilder builder)
        {
            return builder.UseSerilog((context, services, config) =>
            {
                config
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "Backbone")
                    .WriteTo.Console()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);

                if (context.HostingEnvironment.IsDevelopment())
                {
                    config.WriteTo.Debug();
                }
            });
        }
    }
}