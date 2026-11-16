using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;

namespace Tempest.Application.Products.Queries;

/// <summary>Every product (active and inactive) for the admin table.</summary>
public sealed record GetAdminProductsQuery : IQuery<IReadOnlyList<AdminProductDto>>;

internal sealed class GetAdminProductsQueryHandler(IProductRepository products)
    : IQueryHandler<GetAdminProductsQuery, IReadOnlyList<AdminProductDto>>
{
    public async Task<Result<IReadOnlyList<AdminProductDto>>> Handle(
        GetAdminProductsQuery request,
        CancellationToken cancellationToken)
    {
        var all = await products.ListAllAsync(cancellationToken);
        IReadOnlyList<AdminProductDto> items = [.. all.Select(product => product.ToAdminDto())];
        return Result.Success(items);
    }
}
