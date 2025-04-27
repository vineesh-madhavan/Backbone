
// Infrastructure/Persistence/ApplicationDbContext.cs
using Backbone.Core.Entities;
using Backbone.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backbone.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ApplicationLog> ApplicationLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql()
                    .UseSnakeCaseNamingConvention() // Optional: for snake_case naming
                    //.EnableSensitiveDataLogging() // Only in development
                    .EnableDetailedErrors(); // Better error messages
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // PostgreSQL specific configurations
            modelBuilder.HasPostgresExtension("uuid-ossp");

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Configure BaseEntity properties
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.CreatedAt))
                    .HasDefaultValueSql("NOW()");

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.UpdatedAt))
                    .IsRequired(false);
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
