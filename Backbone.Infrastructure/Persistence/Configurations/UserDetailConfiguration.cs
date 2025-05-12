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
    public class UserDetailConfiguration : IEntityTypeConfiguration<UserDetail>
    {
        public void Configure(EntityTypeBuilder<UserDetail> builder)
        {
            // Table Configuration
            builder.ToTable("user_details");

            // Primary Key
            builder.HasKey(ud => ud.UserDetailId)
                   .HasName("pk_user_details");

            // Property Configurations
            builder.Property(ud => ud.UserDetailId)
                   .HasColumnName("user_detail_id")
                   .UseIdentityAlwaysColumn();

            builder.Property(ud => ud.Email)
                   .HasColumnName("user_email")
                   .HasMaxLength(255);

            // Audit Fields
            builder.Property(ud => ud.LastModifiedAt)
                   .HasColumnName("last_modified_at")
                   .HasPrecision(6);

            // Soft Delete Fields
            builder.Property(ud => ud.IsDeleted)
                   .HasColumnName("is_deleted")
                   .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(ud => ud.Email)
                   .HasDatabaseName("ix_user_details_email");

            builder.HasIndex(ud => ud.UserId)
                   .HasDatabaseName("ix_user_details_user_id");

            // Relationships
            builder.HasOne(ud => ud.User)
                   .WithMany(u => u.UserDetails)
                   .HasForeignKey(ud => ud.UserId)
                   .HasConstraintName("fk_user_details_user");

            // Query Filter for Soft Delete
            builder.HasQueryFilter(ud => !ud.IsDeleted);

            // PostgreSQL Specific Optimizations
            builder.Property(ud => ud.DateOfBirth)
                   .HasColumnType("date");
        }
    }
}
