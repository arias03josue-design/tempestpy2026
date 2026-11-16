using Tempest.Domain.Entities;
using Tempest.Domain.Enums;

namespace Tempest.Application.Products;

/// <summary>Public catalog card (only active products).</summary>
public sealed record CatalogProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string ImageUrl,
    Rarity Rarity);

/// <summary>Row of the admin product table.</summary>
public sealed record AdminProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string ImageUrl,
    Rarity Rarity,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

internal static class ProductMappings
{
    public static CatalogProductDto ToCatalogDto(this Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.ImageUrl,
        product.Rarity);

    public static AdminProductDto ToAdminDto(this Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.ImageUrl,
        product.Rarity,
        product.IsActive,
        product.CreatedAtUtc,
        product.UpdatedAtUtc);
}
