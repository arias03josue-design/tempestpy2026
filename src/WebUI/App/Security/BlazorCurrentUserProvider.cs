using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Tempest.Application.Common.Security;
using Tempest.WebUI.Entities.Session;

namespace Tempest.WebUI.App.Security;

/// <summary>
/// Reads the caller identity for the Application layer. Inside Blazor (SSR or circuit) the source of
/// truth is the <see cref="AuthenticationStateProvider"/>; HttpContext is only a fallback for
/// plain endpoints and middleware.
/// </summary>
internal sealed class BlazorCurrentUserProvider(
    AuthenticationStateProvider authenticationStateProvider,
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUserProvider
{
    public async ValueTask<CurrentUser> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal? principal;
        try
        {
            principal = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        }
        catch (InvalidOperationException)
        {
            // Outside a Blazor render the provider has no state yet.
            principal = httpContextAccessor.HttpContext?.User;
        }

        return SessionClaims.ToCurrentUser(principal);
    }
}
