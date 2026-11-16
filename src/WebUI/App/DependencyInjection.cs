using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Security;
using Tempest.Infrastructure.Storage;
using Tempest.WebUI.App.Diagnostics;
using Tempest.WebUI.App.Security;
using Tempest.WebUI.Shared.Config;
using Tempest.WebUI.Shared.Formatting;
using Tempest.WebUI.Shared.UI;

namespace Tempest.WebUI.App;

/// <summary>Composition root of the presentation layer (services + HTTP pipeline).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWebUI(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddRazorComponents().AddInteractiveServerComponents();

        // Authentication: users live in SQLite, the session is an HttpOnly cookie.
        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, SessionRevalidatingAuthenticationStateProvider>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserProvider, BlazorCurrentUserProvider>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthentication.Configure);
        services.AddAuthorization();
        services.AddLoginRateLimiting();

        // The keys that encrypt auth cookies and antiforgery tokens must survive container restarts.
        var keysPath = Path.GetFullPath(configuration["DataProtection:KeysPath"] ?? "data/keys", environment.ContentRootPath);
        services.AddDataProtection()
            .SetApplicationName("Tempest.Store")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        services.AddOptions<StoreOptions>()
            .Bind(configuration.GetSection(StoreOptions.SectionName))
            .Validate(options => StoreOptions.IsKnownCulture(options.Culture), "Store:Culture is not a valid culture name.")
            .ValidateOnStart();
        services.AddSingleton<PriceFormatter>();
        services.AddScoped<ToastService>();

        // Dev aid: delays queries so loading skeletons can be seen (SQLite answers in ~1 ms).
        if (configuration.GetValue("Diagnostics:SimulatedQueryLatencyMs", 0) > 0)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SimulatedLatencyBehavior<,>));
        }

        return services;
    }

    public static WebApplication UseWebUI(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
            app.UseHsts();
        }

        // TLS is expected to be terminated by a reverse proxy in front of the container, so no HTTPS redirection here.
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseSecurityHeaders();
        app.UseUploadedImages();

        // Explicit routing *after* the uploads: otherwise endpoint matching runs first and (in Development)
        // MapStaticAssets' fallback endpoint would claim /uploads/* and skip the caching headers.
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.MapAuthEndpoints();
        app.MapBackupEndpoints();
        app.MapHealthChecks("/healthz");

        return app;
    }

    private static void UseSecurityHeaders(this WebApplication app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            await next(context);
        });

    /// <summary>
    /// Uploaded images are written at runtime (Docker volume), so they are not part of the build-time
    /// static assets manifest: serve the folder with its own provider.
    /// </summary>
    private static void UseUploadedImages(this WebApplication app)
    {
        var storage = app.Services.GetRequiredService<IOptions<StorageOptions>>().Value;
        Directory.CreateDirectory(storage.UploadsPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(storage.UploadsPath),
            RequestPath = storage.PublicBasePath,
            OnPrepareResponse = context =>
                // Random, never reused file names: safe to cache forever.
                context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable",
        });
    }
}
