using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Tempest.Application.Users;
using Tempest.WebUI.Entities.Session;

namespace Tempest.WebUI.App.Security;

internal static class CookieAuthentication
{
    public static void Configure(CookieAuthenticationOptions options)
    {
        options.Cookie.Name = "tempest.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Secure when the request is HTTPS (reverse proxy + ASPNETCORE_FORWARDEDHEADERS_ENABLED).
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = ValidateSessionAsync;
    }

    /// <summary>
    /// Rejects cookies whose security stamp no longer matches the database: after a password change
    /// every other session is signed out on its next request. Only admins carry this cookie, and the
    /// check is a primary-key lookup, so it is cheap.
    /// </summary>
    private static async Task ValidateSessionAsync(CookieValidatePrincipalContext context)
    {
        if (SessionClaims.TryRead(context.Principal, out var session))
        {
            var validator = context.HttpContext.RequestServices.GetRequiredService<IUserSessionValidator>();
            if (await validator.IsValidAsync(session.UserId, session.SecurityStamp, context.HttpContext.RequestAborted))
            {
                return;
            }
        }

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
