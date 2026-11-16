using Tempest.Domain.Common;
using Tempest.Domain.Enums;

namespace Tempest.Domain.Entities;

/// <summary>
/// Catalog item. Inactive products stay manageable from the admin panel
/// but are hidden from the public catalog.
/// </summary>
public sealed class Product : Entity<Guid>
{
    public const int NameMaxLength = 120;
    public const int DescriptionMaxLength = 500;
    public const int ImageUrlMaxLength = 2048;
    public const decimal MinPrice = 0.01m;
    public const decimal MaxPrice = 9_999_999.99m;

    private Product()
    {
        // Required by EF Core.
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    /// <summary>Absolute http(s) URL or app-relative path (e.g. <c>/uploads/…</c>).</summary>
    public string ImageUrl { get; private set; } = string.Empty;

    public Rarity Rarity { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static Product Create(
        string name,
        string? description,
        decimal price,
        string imageUrl,
        Rarity rarity,
        bool isActive,
        DateTime utcNow)
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            IsActive = isActive,
            CreatedAtUtc = utcNow,
        };

        product.SetDetails(name, description, price, imageUrl, rarity);
        return product;
    }

    public void Update(
        string name,
        string? description,
        decimal price,
        string imageUrl,
        Rarity rarity,
        bool isActive,
        DateTime utcNow)
    {
        SetDetails(name, description, price, imageUrl, rarity);
        IsActive = isActive;
        UpdatedAtUtc = utcNow;
    }

    public void SetActive(bool isActive, DateTime utcNow)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        UpdatedAtUtc = utcNow;
    }

    public static bool IsValidPrice(decimal price) =>
        price is >= MinPrice and <= MaxPrice && decimal.Round(price, 2) == price;

    /// <summary>Accepts absolute http(s) URLs and app-relative paths, never protocol-relative ones (<c>//host</c>).</summary>
    public static bool IsValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > ImageUrlMaxLength)
        {
            return false;
        }

        if (url[0] == '/')
        {
            return url.Length > 1 && url[1] is not ('/' or '\\') && !url.Any(char.IsWhiteSpace);
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    private void SetDetails(string name, string? description, decimal price, string imageUrl, Rarity rarity)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > NameMaxLength)
        {
            throw new DomainException(
                "product.invalid_name",
                $"El nombre es obligatorio y admite hasta {NameMaxLength} caracteres.");
        }

        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (description?.Length > DescriptionMaxLength)
        {
            throw new DomainException(
                "product.invalid_description",
                $"La descripción admite hasta {DescriptionMaxLength} caracteres.");
        }

        if (!IsValidPrice(price))
        {
            throw new DomainException(
                "product.invalid_price",
                "El precio debe ser mayor a 0, menor a 10 millones y tener como máximo 2 decimales.");
        }

        imageUrl = imageUrl?.Trim() ?? string.Empty;
        if (!IsValidImageUrl(imageUrl))
        {
            throw new DomainException(
                "product.invalid_image",
                "La imagen debe ser una URL http(s) o una ruta local que empiece con '/'.");
        }

        if (!Enum.IsDefined(rarity))
        {
            throw new DomainException("product.invalid_rarity", "La rareza seleccionada no es válida.");
        }

        Name = name;
        Description = description;
        Price = price;
        ImageUrl = imageUrl;
        Rarity = rarity;
    }
}
