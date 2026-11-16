using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;

namespace Tempest.Application.Maintenance.Queries;

public sealed record DatabaseInfoDto(long DatabaseBytes, long UploadsBytes, int UploadFiles, int Products, int Users)
{
    public long TotalBytes => DatabaseBytes + UploadsBytes;
}

/// <summary>Size of the database (including its WAL file) and of the uploaded images.</summary>
public sealed record GetDatabaseInfoQuery : IQuery<DatabaseInfoDto>;

internal sealed class GetDatabaseInfoQueryHandler(
    IDatabaseMaintenance maintenance,
    IProductRepository products,
    IUserRepository users)
    : IQueryHandler<GetDatabaseInfoQuery, DatabaseInfoDto>
{
    public async Task<Result<DatabaseInfoDto>> Handle(GetDatabaseInfoQuery request, CancellationToken cancellationToken)
    {
        var storage = await maintenance.GetStorageInfoAsync(cancellationToken);
        var productCount = (await products.ListAllAsync(cancellationToken)).Count;
        var userCount = (await users.ListAsync(cancellationToken)).Count;

        return new DatabaseInfoDto(storage.DatabaseBytes, storage.UploadsBytes, storage.UploadFiles, productCount, userCount);
    }
}
