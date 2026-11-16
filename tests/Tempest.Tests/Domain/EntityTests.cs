using Tempest.Domain.Common;
using Tempest.Domain.Entities;
using Tempest.Domain.Enums;
using Tempest.Domain.ValueObjects;

namespace Tempest.Tests.Domain;

public sealed class ProductTests
{
    private static readonly DateTime Now = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_trims_text_and_keeps_the_given_state()
    {
        var product = Product.Create("  Katana  ", "  ", 149m, "/uploads/x.png", Rarity.Epic, isActive: false, Now);

        Assert.Equal("Katana", product.Name);
        Assert.Null(product.Description);
        Assert.False(product.IsActive);
        Assert.Equal(Now, product.CreatedAtUtc);
        Assert.NotEqual(Guid.Empty, product.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.001)]       // more than 2 decimals
    [InlineData(10_000_000)]   // above the maximum
    public void Rejects_invalid_prices(double price) =>
        Assert.Throws<DomainException>(() =>
            Product.Create("Katana", null, (decimal)price, "/uploads/x.png", Rarity.Common, true, Now));

    [Theory]
    [InlineData("/uploads/abc.png", true)]
    [InlineData("/images/products/kitsune-ronin.svg", true)]
    [InlineData("https://cdn.example.com/figura.webp", true)]
    [InlineData("//evil.example.com/x.png", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:image/png;base64,AAAA", false)]
    [InlineData("", false)]
    public void Validates_image_urls(string url, bool expected) => Assert.Equal(expected, Product.IsValidImageUrl(url));

    [Fact]
    public void SetActive_only_touches_the_timestamp_when_the_state_changes()
    {
        var product = Product.Create("Katana", null, 149m, "/x.png", Rarity.Epic, isActive: true, Now);

        product.SetActive(true, Now.AddHours(1));
        Assert.Null(product.UpdatedAtUtc);

        product.SetActive(false, Now.AddHours(2));
        Assert.False(product.IsActive);
        Assert.Equal(Now.AddHours(2), product.UpdatedAtUtc);
    }
}

public sealed class UserTests
{
    [Fact]
    public void ChangePassword_rotates_the_security_stamp_and_clears_the_temporary_flag()
    {
        var user = User.Create(Username.Create("admin"), "hash-1", mustChangePassword: true, DateTime.UtcNow);
        var stamp = user.SecurityStamp;

        user.ChangePassword("hash-2", DateTime.UtcNow);

        Assert.Equal("hash-2", user.PasswordHash);
        Assert.NotEqual(stamp, user.SecurityStamp);
        Assert.False(user.MustChangePassword);
        Assert.NotNull(user.PasswordChangedAtUtc);
    }

    [Fact]
    public void Upgrading_the_hash_keeps_existing_sessions()
    {
        var user = User.Create(Username.Create("admin"), "old-hash", mustChangePassword: false, DateTime.UtcNow);
        var stamp = user.SecurityStamp;

        user.UpgradePasswordHash("stronger-hash");

        Assert.Equal(stamp, user.SecurityStamp);
    }
}
