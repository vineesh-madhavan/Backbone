
// Infrastructure/Persistence/ApplicationDbContext.cs
using Backbone.Core.Entities;
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
            base.OnModelCreating(modelBuilder);
        }
    }
}
