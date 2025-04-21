using Backbone.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;

namespace Backbone.Infrastructure.Persistence
{
    public static class DatabaseMigration
    {
        public static IApplicationBuilder UseMigrations(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            
            var logger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("DatabaseMigration");
            try
            {
                logger.LogInformation("Starting database migration...");

                // Get required services
                var configuration = services.GetRequiredService<IConfiguration>();
                var dbContext = services.GetRequiredService<ApplicationDbContext>();

                var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                              configuration.GetSection("ConnectionStrings:DefaultConnection").Value ??
                                              throw new InvalidOperationException("Connection string not found");

                // Enhanced retry policy with logging
                Policy
                    .Handle<Npgsql.PostgresException>()
                    .Or<InvalidOperationException>()
                    .WaitAndRetry(
                        retryCount: 5,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(10),
                        onRetry: (exception, delay, retryCount, context) =>
                        {
                            logger.LogWarning(
                                "Migration attempt {RetryCount} failed. Retrying in {Delay}. Error: {Message}",
                                retryCount, delay, exception.Message);
                        })
                    .Execute(() =>
                    {

                        // First ensure database exists
                        if (!dbContext.Database.CanConnect())
                        {
                            logger.LogWarning("Database does not exist or is not accessible");
                            throw new Exception("Database connection failed");
                        }

                        dbContext.Database.Migrate();
                    });

                logger.LogInformation("Database migration completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database migration failed");
                throw;

            }
            return app;
        }
    }
}