using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;

namespace Tempest.Application.Products.Queries;

/// <summary>Active products shown on the public catalog (<c>/</c>).</summary>
public sealed record GetPublicCatalogQuery : IQuery<IReadOnlyList<CatalogProductDto>>, IAllowAnonymous;

internal sealed class GetPublicCatalogQueryHandler(IProductRepository products)
    : IQueryHandler<GetPublicCatalogQuery, IReadOnlyList<CatalogProductDto>>
{
    public async Task<Result<IReadOnlyList<CatalogProductDto>>> Handle(
        GetPublicCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var active = await products.ListActiveAsync(cancellationToken);
        IReadOnlyList<CatalogProductDto> items = [.. active.Select(product => product.ToCatalogDto())];
        return Result.Success(items);
    }
}
