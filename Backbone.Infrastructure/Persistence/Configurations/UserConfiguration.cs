// Infrastructure/Persistence/UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Backbone.Core.Entities;

namespace Backbone.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Table Configuration
            builder.ToTable("users");

            // Primary Key
            builder.HasKey(u => u.UserId)
                   .HasName("pk_users_user_id");

            // Property Configurations
            builder.Property(u => u.UserId)
                   .HasColumnName("user_id")
                   .UseIdentityAlwaysColumn();

            builder.Property(u => u.UserName)
                   .HasColumnName("user_name")
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(u => u.PasswordHash)
                   .HasColumnName("password_hash")
                   .HasColumnType("bytea");

            // Audit Fields
            builder.Property(u => u.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("NOW()");

            builder.Property(u => u.CreatedBy)
                   .HasColumnName("created_by")
                   .HasMaxLength(100);

            // Soft Delete Fields
            builder.Property(u => u.IsDeleted)
                   .HasColumnName("is_deleted")
                   .HasDefaultValue(false);

            builder.Property(u => u.DeletedAt)
                   .HasColumnName("deleted_at")
                   .IsRequired(false);

            // Indexes
            builder.HasIndex(u => u.UserName)
                   .HasDatabaseName("ix_users_username")
                   .IsUnique();

            builder.HasIndex(u => u.StatusId)
                   .HasDatabaseName("ix_users_status_id");

            // Relationships
            builder.HasOne(u => u.Status)
                   .WithMany(s => s.Users)
                   .HasForeignKey(u => u.StatusId)
                   .HasConstraintName("fk_users_status");

            // Query Filter for Soft Delete
            builder.HasQueryFilter(u => !u.IsDeleted);

            // PostgreSQL Specific Optimizations
            builder.Property(u => u.CreatedAt)
                   .HasPrecision(6); // Microsecond precision
        }
    }
}