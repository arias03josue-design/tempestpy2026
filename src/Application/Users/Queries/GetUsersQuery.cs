using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;

namespace Tempest.Application.Users.Queries;

public sealed record GetUsersQuery : IQuery<IReadOnlyList<UserDto>>;

internal sealed class GetUsersQueryHandler(IUserRepository users)
    : IQueryHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var all = await users.ListAsync(cancellationToken);
        IReadOnlyList<UserDto> items = [.. all.Select(user => user.ToDto())];
        return Result.Success(items);
    }
}
