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
    public class UserStatusConfiguration : IEntityTypeConfiguration<UserStatus>
    {
        public void Configure(EntityTypeBuilder<UserStatus> builder)
        {
            // Table Configuration
            builder.ToTable("user_status", "user_management");

            // Primary Key
            builder.HasKey(us => us.UserStatusId)
                   .HasName("pk_user_status");

            // Property Configurations
            builder.Property(us => us.StatusName)
                   .HasColumnName("status_name")
                   .HasMaxLength(50);

            // PostgreSQL Specific Optimizations
            builder.HasData(
                new UserStatus { UserStatusId = 1, StatusName = "Active" },
                new UserStatus { UserStatusId = 2, StatusName = "Inactive" },
                new UserStatus { UserStatusId = 3, StatusName = "Suspended" }
            );
        }
    }
}
