namespace Tempest.WebUI.Shared.Navigation;

public static class SafeRedirect
{
    /// <summary>
    /// Returns <paramref name="url"/> only when it is a local path such as <c>/admin</c>
    /// (prevents open redirects to <c>//evil.com</c> or <c>https://…</c>).
    /// </summary>
    public static string ToLocal(string? url, string fallback)
    {
        if (string.IsNullOrWhiteSpace(url) || url[0] != '/')
        {
            return fallback;
        }

        if (url.Length > 1 && url[1] is '/' or '\\')
        {
            return fallback;
        }

        return url.Any(char.IsControl) ? fallback : url;
    }
}
