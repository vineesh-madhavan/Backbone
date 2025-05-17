// Backbone.Infrastructure/Logging/SerilogExtensions.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;
using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Enrichers;

namespace Backbone.Infrastructure.Logging
{
    public static class SerilogExtensions
    {
        public static IHostBuilder UseCustomSerilog(this IHostBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.UseSerilog((context, services, config) =>
            {
                try
                {
                    var loggerFactory = services.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("SerilogExtensions");
                    logger?.LogInformation("Configuring Serilog logging");

                    // Base configuration
                    config
                        .MinimumLevel.Information()
                        .Enrich.FromLogContext()
                        .Enrich.WithProperty("Application", "Backbone")
                        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

                    // Console sink with structured logging
                    config.WriteTo.Console(new CompactJsonFormatter());

                    // File sink with daily rolling and JSON format
                    config.WriteTo.File(
                        formatter: new CompactJsonFormatter(),
                        path: "logs/log-.json",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7);

                    // Debug sink for development
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        config.WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
                        logger?.LogDebug("Added Debug sink for development environment");
                    }

                    logger?.LogInformation("Serilog configuration completed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to configure Serilog: {ex}");
                    throw new InvalidOperationException("Serilog configuration failed", ex);
                }
            });
        }
    }
}