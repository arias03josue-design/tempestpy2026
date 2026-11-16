using FluentValidation;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Results;
using Tempest.Domain.Entities;
using Tempest.Domain.Enums;

namespace Tempest.Application.Products;

/// <summary>Editable fields shared by the create and update commands.</summary>
public interface IProductData
{
    string Name { get; }

    string? Description { get; }

    decimal Price { get; }

    string ImageUrl { get; }

    Rarity Rarity { get; }

    bool IsActive { get; }
}

internal sealed class ProductDataValidator : AbstractValidator<IProductData>
{
    public ProductDataValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(Product.NameMaxLength).WithMessage($"El nombre admite hasta {Product.NameMaxLength} caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(Product.DescriptionMaxLength)
            .WithMessage($"La descripción admite hasta {Product.DescriptionMaxLength} caracteres.");

        RuleFor(x => x.Price)
            .Must(Product.IsValidPrice)
            .WithMessage("Ingresa un precio mayor a 0 con hasta 2 decimales.");

        RuleFor(x => x.ImageUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Agrega una imagen: pega una URL o sube un archivo.")
            .Must(Product.IsValidImageUrl).WithMessage("Usa una URL http(s) válida o una ruta local que empiece con '/'.");

        RuleFor(x => x.Rarity)
            .IsInEnum().WithMessage("Selecciona una rareza válida.");
    }
}

internal static class ProductErrors
{
    public static readonly Error NotFound =
        Error.NotFound("product.not_found", "El producto no existe o ya fue eliminado.");
}

internal static class ProductImageCleanup
{
    /// <summary>Deletes a locally uploaded image once no product references it anymore.</summary>
    public static async Task DeleteIfOrphanedAsync(
        this IImageStorage storage,
        IProductRepository products,
        string imageUrl,
        CancellationToken cancellationToken)
    {
        if (!storage.IsManaged(imageUrl) || await products.IsImageInUseAsync(imageUrl, cancellationToken))
        {
            return;
        }

        await storage.DeleteAsync(imageUrl, cancellationToken);
    }
}
