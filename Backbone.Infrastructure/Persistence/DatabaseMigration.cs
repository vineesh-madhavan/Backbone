//Backbone.Infrastructure.Persistence/DatabaseMigration.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using System;
using System.Diagnostics;

namespace Backbone.Infrastructure.Persistence
{
    public static class DatabaseMigration
    {
        private class MigrationLogger  // Inner class for logger context
        {
        }

        public static IApplicationBuilder UseMigrations(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            // Use the inner class as logger context
            var logger = services.GetRequiredService<ILogger<MigrationLogger>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            try
            {
                logger.LogInformation("Starting database migration process");

                var stopwatch = Stopwatch.StartNew();
                var connectionString = GetConnectionString(configuration, logger);

                ExecuteMigrationWithRetry(dbContext, connectionString, logger);

                stopwatch.Stop();
                logger.LogInformation("Database migration completed successfully in {ElapsedMilliseconds}ms",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database migration process failed");
                throw new DatabaseMigrationException("Database migration failed", ex);
            }

            return app;
        }

        private static string GetConnectionString(IConfiguration configuration, ILogger<MigrationLogger> logger)
        {
            try
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                     configuration.GetSection("ConnectionStrings:DefaultConnection").Value;

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    logger.LogError("Connection string configuration is missing");
                    throw new InvalidOperationException("Database connection string not found in configuration");
                }

                logger.LogDebug("Retrieved database connection string");
                return connectionString;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve connection string");
                throw;
            }
        }

        private static void ExecuteMigrationWithRetry(
            ApplicationDbContext dbContext,
            string connectionString,
            ILogger<MigrationLogger> logger)
        {
            var policy = Policy
                .Handle<NpgsqlException>()
                .Or<DbUpdateException>()
                .Or<InvalidOperationException>()
                .WaitAndRetry(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, delay, retryCount, context) =>
                    {
                        logger.LogWarning(
                            "Migration attempt {RetryCount} failed. Retrying in {DelaySeconds}s. Error: {ErrorType}: {ErrorMessage}",
                            retryCount,
                            delay.TotalSeconds,
                            exception.GetType().Name,
                            exception.Message);
                    });

            policy.Execute(() =>
            {
                try
                {
                    logger.LogDebug("Attempting to connect to database");

                    if (!dbContext.Database.CanConnect())
                    {
                        logger.LogError("Database connection test failed");
                        throw new DatabaseConnectionException("Could not establish connection to database");
                    }

                    logger.LogInformation("Applying pending migrations...");
                    dbContext.Database.Migrate();
                    logger.LogDebug("Migrations applied successfully");

                    return true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Migration attempt failed");
                    throw;
                }
            });
        }
    }

    public class DatabaseMigrationException : Exception
    {
        public DatabaseMigrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class DatabaseConnectionException : Exception
    {
        public DatabaseConnectionException(string message)
            : base(message)
        {
        }
    }
}