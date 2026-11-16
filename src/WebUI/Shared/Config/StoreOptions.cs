using System.Globalization;

namespace Tempest.WebUI.Shared.Config;

/// <summary>Store branding and locale (<c>Store</c> section).</summary>
public sealed class StoreOptions
{
    public const string SectionName = "Store";

    public string Name { get; set; } = "TEMPEST";

    public string Tagline { get; set; } = "Anime · Gaming Store";

    /// <summary>Culture used to format prices (currency symbol, separators), e.g. es-MX, es-CR, es-ES.</summary>
    public string Culture { get; set; } = "es-MX";

    public static bool IsKnownCulture(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        try
        {
            return !CultureInfo.GetCultureInfo(name).Equals(CultureInfo.InvariantCulture);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
