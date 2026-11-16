namespace Tempest.Application.Common.Security;

/// <summary>Identity of the caller of a use case.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Username { get; }

    bool IsAuthenticated { get; }
}

/// <summary>Immutable snapshot of the caller identity.</summary>
public sealed record CurrentUser(Guid? UserId, string? Username) : ICurrentUser
{
    public static CurrentUser Anonymous { get; } = new(null, null);

    public bool IsAuthenticated => UserId is not null;
}

/// <summary>
/// Port implemented by the presentation layer: it knows where the identity comes from
/// (auth cookie, Blazor circuit state…).
/// </summary>
public interface ICurrentUserProvider
{
    ValueTask<CurrentUser> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

/// <summary>Per-request holder populated by the dispatcher and injected into handlers as <see cref="ICurrentUser"/>.</summary>
internal sealed class CurrentUserAccessor : ICurrentUser
{
    public CurrentUser User { get; set; } = CurrentUser.Anonymous;

    public Guid? UserId => User.UserId;

    public string? Username => User.Username;

    public bool IsAuthenticated => User.IsAuthenticated;
}
