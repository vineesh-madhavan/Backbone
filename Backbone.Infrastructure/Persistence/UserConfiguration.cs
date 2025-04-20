// Infrastructure/Persistence/UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Backbone.Core.Entities;

namespace Infrastructure.Persistence
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            //builder.Property(u => u.Email)
            //    .IsRequired()
            //    .HasMaxLength(100);

            //builder.Property(u => u.PasswordHash)
            //    .IsRequired();

            //builder.Property(u => u.PasswordSalt)
            //    .IsRequired();

            //builder.Property(u => u.CreatedAt)
            //    .IsRequired();
        }
    }
}