using FluentValidation;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;

namespace Tempest.Application.Products.Commands;

/// <summary>Activates/deactivates a product. Idempotent: sends the desired state, not a toggle.</summary>
public sealed record SetProductStatusCommand(Guid Id, bool IsActive) : ICommand;

internal sealed class SetProductStatusCommandValidator : AbstractValidator<SetProductStatusCommand>
{
    public SetProductStatusCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

internal sealed class SetProductStatusCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<SetProductStatusCommand>
{
    public async Task<Result> Handle(SetProductStatusCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        product.SetActive(request.IsActive, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
