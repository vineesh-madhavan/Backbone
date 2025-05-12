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
    public class DistrictConfiguration : IEntityTypeConfiguration<District>
    {
        public void Configure(EntityTypeBuilder<District> builder)
        {
            // Table Configuration
            builder.ToTable("districts", "user_management");

            // Primary Key
            builder.HasKey(d => d.DistrictId)
                   .HasName("pk_districts");

            // Property Configurations
            builder.Property(d => d.DistrictName)
                   .HasColumnName("district_name")
                   .HasMaxLength(100);

            // Relationships
            builder.HasOne(d => d.State)
                   .WithMany(s => s.Districts)
                   .HasForeignKey(d => d.StateId)
                   .HasConstraintName("fk_districts_state");

            // PostgreSQL Specific Optimizations
            builder.HasIndex(d => d.DistrictName)
                   .HasMethod("gin")
                   .HasDatabaseName("ix_districts_name_gin");
                   //.IsTsVectorExpression("english");
        }
    }
}
