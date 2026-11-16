using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Tempest.Application.Users;
using Tempest.WebUI.Entities.Session;

namespace Tempest.WebUI.App.Security;

/// <summary>
/// An interactive circuit keeps the identity it was opened with. This provider re-checks the
/// security stamp every minute, so a password change also ends already-open admin tabs.
/// </summary>
internal sealed class SessionRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        if (!SessionClaims.TryRead(authenticationState.User, out var session))
        {
            return false;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var validator = scope.ServiceProvider.GetRequiredService<IUserSessionValidator>();
        return await validator.IsValidAsync(session.UserId, session.SecurityStamp, cancellationToken);
    }
}
