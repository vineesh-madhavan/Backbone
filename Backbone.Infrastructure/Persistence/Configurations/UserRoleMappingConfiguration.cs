using Backbone.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backbone.Infrastructure.Persistence.Configurations
{
    public class UserRoleMappingConfiguration : IEntityTypeConfiguration<UserRoleMapping>
    {
        public void Configure(EntityTypeBuilder<UserRoleMapping> builder)
        {
            // Table Configuration
            builder.ToTable("user_role_mappings", "user_management");

            // Primary Key
            builder.HasKey(urm => urm.RoleMapId)
                   .HasName("pk_user_role_mappings");

            // Property Configurations
            builder.Property(urm => urm.RoleMapId)
                   .HasColumnName("role_map_id")
                   .UseIdentityAlwaysColumn();

            // Indexes
            builder.HasIndex(urm => new { urm.UserId, urm.RoleId })
                   .HasDatabaseName("ix_user_role_mappings_unique")
                   .IsUnique();

            // Relationships
            builder.HasOne(urm => urm.Role)
                   .WithMany(r => r.UserRoleMappings)
                   .HasForeignKey(urm => urm.RoleId)
                   .HasConstraintName("fk_user_role_mappings_role");

            // PostgreSQL Specific Optimizations
            builder.Property(urm => urm.CreatedAt)
                   .HasDefaultValueSql("NOW()")
                   .ValueGeneratedOnAdd();
        }
    }
}
