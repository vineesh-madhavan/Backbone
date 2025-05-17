
// Infrastructure/Persistence/ApplicationDbContext.cs
using Backbone.Core.Entities;
using Backbone.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Backbone.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ILogger<ApplicationDbContext> _logger;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ILogger<ApplicationDbContext> logger = null)
            : base(options)
        {
            _logger = logger;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ApplicationLog> ApplicationLogs { get; set; }
        public DbSet<UserDetail> UserDetails { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<UserRoleMapping> UserRoleMappings { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserStatus> UserStatuses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                _logger?.LogDebug("Configuring DbContext options");

                if (!optionsBuilder.IsConfigured)
                {
                    optionsBuilder
                        .UseNpgsql()
                        .UseSnakeCaseNamingConvention()
                        .EnableDetailedErrors();

                    _logger?.LogInformation("DbContext configured with PostgreSQL provider");
                }

                // Add logging only in development
                if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
                {
                    optionsBuilder.LogTo(message => _logger.LogDebug(message))
                                 .EnableSensitiveDataLogging();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to configure DbContext options");
                throw;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            using var _ = _logger?.BeginScope("ModelCreating");

            try
            {
                _logger?.LogInformation("Starting model creation");

                // PostgreSQL specific configurations
                modelBuilder.HasPostgresExtension("uuid-ossp")
                    .HasPostgresExtension("pgcrypto");

                _logger?.LogDebug("Applied PostgreSQL extensions");

                // Apply all configurations from assembly
                modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
                _logger?.LogDebug("Applied entity configurations from assembly");

                // Configure BaseEntity properties
                foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                    .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(BaseEntity.CreatedAt))
                        .HasDefaultValueSql("NOW()");

                    _logger?.LogTrace("Configured BaseEntity properties for {EntityType}", entityType.ClrType.Name);
                }

                base.OnModelCreating(modelBuilder);
                _logger?.LogInformation("Completed model creation successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during model creation");
                throw;
            }
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            using var _ = _logger?.BeginScope("SaveChanges");

            try
            {
                _logger?.LogDebug("Starting to save changes");
                var stopwatch = Stopwatch.StartNew();

                var result = base.SaveChanges(acceptAllChangesOnSuccess);

                stopwatch.Stop();
                _logger?.LogInformation("Successfully saved {Count} changes in {ElapsedMilliseconds}ms",
                    result, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save changes");
                throw;
            }
        }

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            using var _ = _logger?.BeginScope("SaveChangesAsync");

            try
            {
                _logger?.LogDebug("Starting to save changes asynchronously");
                var stopwatch = Stopwatch.StartNew();

                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

                stopwatch.Stop();
                _logger?.LogInformation("Successfully saved {Count} changes in {ElapsedMilliseconds}ms",
                    result, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save changes asynchronously");
                throw;
            }
        }
    }
}
