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
    public class StateConfiguration : IEntityTypeConfiguration<State>
    {
        public void Configure(EntityTypeBuilder<State> builder)
        {
            // Table Configuration
            builder.ToTable("states", "user_management");

            // Primary Key
            builder.HasKey(s => s.StateId)
                   .HasName("pk_states");

            // Property Configurations
            builder.Property(s => s.StateId)
                   .HasColumnName("state_id")
                   .UseIdentityAlwaysColumn();

            // Indexes
            builder.HasIndex(s => s.StateName)
                   .HasDatabaseName("ix_states_name")
                   .IsUnique();

            // PostgreSQL Specific Optimizations
            builder.Property(s => s.StateName)
                   .HasColumnType("varchar(100)");
        }
    }
}
