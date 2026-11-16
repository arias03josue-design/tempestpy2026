using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tempest.Domain.Entities;

namespace Tempest.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(Product.DescriptionMaxLength);
        builder.Property(product => product.ImageUrl).HasMaxLength(Product.ImageUrlMaxLength).IsRequired();

        // SQLite has no decimal type (EF would store TEXT and refuse to sort/compare it in SQL).
        // Money is stored as integer cents: exact, sortable and aggregatable.
        builder.Property(product => product.Price)
            .HasColumnName("PriceCents")
            .HasConversion(price => decimal.ToInt64(price * 100m), cents => cents / 100m);

        builder.Property(product => product.Rarity).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(product => new { product.IsActive, product.CreatedAtUtc });
    }
}
