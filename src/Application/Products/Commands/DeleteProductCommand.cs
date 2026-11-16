using FluentValidation;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;

namespace Tempest.Application.Products.Commands;

public sealed record DeleteProductCommand(Guid Id) : ICommand;

internal sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

internal sealed class DeleteProductCommandHandler(
    IProductRepository products,
    IImageStorage imageStorage,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteProductCommand>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Uploaded files are removed only after the database commit succeeded.
        await imageStorage.DeleteIfOrphanedAsync(products, product.ImageUrl, cancellationToken);
        return Result.Success();
    }
}
