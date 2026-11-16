using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tempest.Domain.Entities;
using Tempest.Domain.ValueObjects;

namespace Tempest.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Username)
            .HasConversion(username => username.Value, value => Username.Create(value))
            .HasMaxLength(Username.MaxLength)
            .IsRequired();

        // Usernames are normalized to lowercase, so this index enforces case-insensitive uniqueness.
        builder.HasIndex(user => user.Username).IsUnique();

        builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(user => user.SecurityStamp).HasMaxLength(64).IsRequired();
    }
}
