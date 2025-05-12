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
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            // Table Configuration
            builder.ToTable("user_roles", "user_management");

            // Primary Key
            builder.HasKey(ur => ur.RoleId)
                   .HasName("pk_user_roles");

            // Property Configurations
            builder.Property(ur => ur.RoleName)
                   .HasColumnName("role_name")
                   .HasMaxLength(50);

            // PostgreSQL Specific Optimizations
            builder.HasData(
                new UserRole { RoleId = 1, RoleName = "Admin" },
                new UserRole { RoleId = 2, RoleName = "User" }
            );
        }
    }
}
