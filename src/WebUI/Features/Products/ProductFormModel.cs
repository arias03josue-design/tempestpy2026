using System.ComponentModel.DataAnnotations;
using Tempest.Application.Products;
using Tempest.Application.Products.Commands;
using Tempest.Domain.Enums;

namespace Tempest.WebUI.Features.Products;

/// <summary>
/// Mutable form model. Property names mirror the commands so server-side (FluentValidation) errors
/// land on the right inputs. DataAnnotations only give instant feedback; the Application layer
/// remains the source of truth.
/// </summary>
public sealed class ProductFormModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción admite hasta 500 caracteres.")]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "9999999.99",
        ErrorMessage = "Ingresa un precio mayor a 0.",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Agrega una imagen: pega una URL o sube un archivo.")]
    public string? ImageUrl { get; set; }

    public Rarity Rarity { get; set; } = Rarity.Common;

    public bool IsActive { get; set; } = true;

    public static ProductFormModel From(AdminProductDto product) => new()
    {
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        ImageUrl = product.ImageUrl,
        Rarity = product.Rarity,
        IsActive = product.IsActive,
    };

    public CreateProductCommand ToCreateCommand() =>
        new(Name, Description, Price, ImageUrl ?? string.Empty, Rarity, IsActive);

    public UpdateProductCommand ToUpdateCommand(Guid id) =>
        new(id, Name, Description, Price, ImageUrl ?? string.Empty, Rarity, IsActive);
}
