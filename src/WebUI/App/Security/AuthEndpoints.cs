using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Tempest.WebUI.Shared.Navigation;

namespace Tempest.WebUI.App.Security;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST only, with antiforgery (validated automatically for form-bound minimal APIs).
        endpoints.MapPost("/auth/logout", async (HttpContext context, [FromForm] string? returnUrl) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return TypedResults.LocalRedirect(SafeRedirect.ToLocal(returnUrl, "/"));
        });

        return endpoints;
    }
}

internal static class LoginRateLimiting
{
    private const int AttemptsPerMinute = 10;

    /// <summary>Throttles login attempts per client IP to slow down brute-force attacks.</summary>
    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                HttpMethods.IsPost(context.Request.Method) && context.Request.Path.StartsWithSegments("/login")
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        $"login:{context.Connection.RemoteIpAddress}",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = AttemptsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        })
                    : RateLimitPartition.GetNoLimiter("unlimited"));

            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status303SeeOther;
                context.HttpContext.Response.Headers.Location = "/login?reason=throttled";
                return ValueTask.CompletedTask;
            };
        });
}
