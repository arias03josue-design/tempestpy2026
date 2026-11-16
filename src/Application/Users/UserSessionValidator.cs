using Tempest.Application.Common.Interfaces;

namespace Tempest.Application.Users;

/// <summary>
/// Checks that a session (auth cookie / Blazor circuit) still belongs to an existing user whose
/// security stamp did not change. Called by the authentication infrastructure, not by pages,
/// so it is a plain service instead of a dispatched query.
/// </summary>
public interface IUserSessionValidator
{
    Task<bool> IsValidAsync(Guid userId, string securityStamp, CancellationToken cancellationToken = default);
}

internal sealed class UserSessionValidator(IUserRepository users) : IUserSessionValidator
{
    public async Task<bool> IsValidAsync(Guid userId, string securityStamp, CancellationToken cancellationToken = default)
    {
        var currentStamp = await users.GetSecurityStampAsync(userId, cancellationToken);
        return currentStamp is not null && string.Equals(currentStamp, securityStamp, StringComparison.Ordinal);
    }
}
