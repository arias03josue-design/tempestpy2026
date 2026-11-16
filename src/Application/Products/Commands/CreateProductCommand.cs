using FluentValidation;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;
using Tempest.Domain.Entities;
using Tempest.Domain.Enums;

namespace Tempest.Application.Products.Commands;

public sealed record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price,
    string ImageUrl,
    Rarity Rarity,
    bool IsActive) : ICommand<Guid>, IProductData;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator() => Include(new ProductDataValidator());
}

internal sealed class CreateProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.ImageUrl,
            request.Rarity,
            request.IsActive,
            timeProvider.GetUtcNow().UtcDateTime);

        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
