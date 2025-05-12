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
    public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
    {
        public void Configure(EntityTypeBuilder<UserAddress> builder)
        {
            // Table Configuration
            builder.ToTable("user_addresses", "user_management");

            // Primary Key
            builder.HasKey(ua => ua.UserAddressId)
                   .HasName("pk_user_addresses");

            // Property Configurations
            builder.Property(ua => ua.UserAddressId)
                   .HasColumnName("user_address_id")
                   .UseIdentityAlwaysColumn();

            builder.Property(ua => ua.PIN)
                   .HasColumnName("user_pin")
                   .HasMaxLength(10);

            // Audit Fields
            builder.Property(ua => ua.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("NOW()");

            // Soft Delete Fields
            builder.Property(ua => ua.DeletedBy)
                   .HasColumnName("deleted_by")
                   .HasMaxLength(100);

            // Indexes
            builder.HasIndex(ua => new { ua.UserId, ua.IsDeleted })
                   .HasDatabaseName("ix_user_addresses_user_deleted");

            // Relationships
            builder.HasOne(ua => ua.District)
                   .WithMany(d => d.UserAddresses)
                   .HasForeignKey(ua => ua.DistrictId)
                   .HasConstraintName("fk_user_addresses_district");

            // Query Filter for Soft Delete
            builder.HasQueryFilter(ua => !ua.IsDeleted);

            // PostgreSQL Specific Optimizations
            builder.Property(ua => ua.AddressLine1)
                   .HasColumnType("varchar(200)");
        }
    }
}
