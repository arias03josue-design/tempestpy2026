using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Tempest.Application.Common.Security;
using Tempest.Application.Users;

namespace Tempest.WebUI.Entities.Session;

/// <summary>The signed-in admin as stored in the auth cookie.</summary>
public readonly record struct UserSession(Guid UserId, string Username, string SecurityStamp);

/// <summary>Maps the authenticated user to/from cookie claims.</summary>
public static class SessionClaims
{
    public const string SecurityStampClaim = "tempest:security_stamp";
    public const string MustChangePasswordClaim = "tempest:must_change_password";

    public static ClaimsPrincipal CreatePrincipal(AuthenticatedUserDto user)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(SecurityStampClaim, user.SecurityStamp),
        ];

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(MustChangePasswordClaim, "true"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static bool TryRead(ClaimsPrincipal? principal, out UserSession session)
    {
        session = default;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var stamp = principal.FindFirstValue(SecurityStampClaim);
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || string.IsNullOrEmpty(stamp))
        {
            return false;
        }

        session = new UserSession(userId, principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty, stamp);
        return true;
    }

    public static CurrentUser ToCurrentUser(ClaimsPrincipal? principal) =>
        TryRead(principal, out var session) ? new CurrentUser(session.UserId, session.Username) : CurrentUser.Anonymous;

    public static Guid? GetUserId(ClaimsPrincipal? principal) =>
        TryRead(principal, out var session) ? session.UserId : null;

    public static bool MustChangePassword(ClaimsPrincipal? principal) =>
        principal?.HasClaim(MustChangePasswordClaim, "true") == true;
}
