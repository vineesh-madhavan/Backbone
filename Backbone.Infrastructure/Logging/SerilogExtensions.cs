// Backbone.Infrastructure/Logging/SerilogExtensions.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using System;

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
                    var logger = services.GetService<ILogger<SerilogExtensions>>();
                    logger?.LogInformation("Configuring Serilog logging");

                    // Base configuration
                    config
                        .MinimumLevel.Information()
                        .Enrich.FromLogContext()
                        .Enrich.WithExceptionDetails()
                        .Enrich.WithProperty("Application", "Backbone")
                        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                        .Enrich.WithMachineName()
                        .Enrich.WithProcessId()
                        .Enrich.WithThreadId();

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

                    // Add additional sinks based on configuration
                    ConfigureAdditionalSinks(context, config, logger);

                    logger?.LogInformation("Serilog configuration completed");
                }
                catch (Exception ex)
                {
                    var logger = services.GetService<ILogger<SerilogExtensions>>();
                    logger?.LogCritical(ex, "Failed to configure Serilog");
                    throw new InvalidOperationException("Serilog configuration failed", ex);
                }
            });
        }

        private static void ConfigureAdditionalSinks(
            HostBuilderContext context,
            LoggerConfiguration config,
            ILogger<SerilogExtensions> logger)
        {
            try
            {
                // Elasticsearch/Kibana configuration example
                var elasticUrl = context.Configuration["ElasticConfiguration:Uri"];
                if (!string.IsNullOrEmpty(elasticUrl))
                {
                    config.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticUrl))
                    {
                        AutoRegisterTemplate = true,
                        IndexFormat = "backbone-logs-{0:yyyy.MM.dd}",
                        ModifyConnectionSettings = x => x.BasicAuthentication(
                            context.Configuration["ElasticConfiguration:Username"],
                            context.Configuration["ElasticConfiguration:Password"])
                    });
                    logger?.LogInformation("Added Elasticsearch sink");
                }

                // Application Insights configuration
                var appInsightsKey = context.Configuration["ApplicationInsights:InstrumentationKey"];
                if (!string.IsNullOrEmpty(appInsightsKey))
                {
                    config.WriteTo.ApplicationInsights(appInsightsKey, TelemetryConverter.Traces);
                    logger?.LogInformation("Added Application Insights sink");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error configuring additional sinks");
                // Continue without additional sinks
            }
        }
    }
}