using FluentValidation;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;
using Tempest.Domain.Enums;

namespace Tempest.Application.Products.Commands;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string ImageUrl,
    Rarity Rarity,
    bool IsActive) : ICommand, IProductData;

internal sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        Include(new ProductDataValidator());
    }
}

internal sealed class UpdateProductCommandHandler(
    IProductRepository products,
    IImageStorage imageStorage,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<UpdateProductCommand>
{
    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        var previousImage = product.ImageUrl;
        product.Update(
            request.Name,
            request.Description,
            request.Price,
            request.ImageUrl,
            request.Rarity,
            request.IsActive,
            timeProvider.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.Equals(previousImage, product.ImageUrl, StringComparison.Ordinal))
        {
            await imageStorage.DeleteIfOrphanedAsync(products, previousImage, cancellationToken);
        }

        return Result.Success();
    }
}
