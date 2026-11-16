using System.Globalization;
using Microsoft.Extensions.Options;
using Tempest.WebUI.Shared.Config;

namespace Tempest.WebUI.Shared.Formatting;

/// <summary>Formats prices with the store culture. The same text is used on the card and in the WhatsApp message.</summary>
public sealed class PriceFormatter(IOptions<StoreOptions> options)
{
    public CultureInfo Culture { get; } = CultureInfo.GetCultureInfo(options.Value.Culture);

    public string Format(decimal price) => price.ToString("C", Culture);

    public string FormatDate(DateTime utc) => utc.ToString("d MMM yyyy", Culture);
}

public static class TimeAgo
{
    /// <summary>Timezone-agnostic relative time ("hace 5 min"), with the date as fallback.</summary>
    public static string Format(DateTime utc, DateTime utcNow, CultureInfo culture)
    {
        var elapsed = utcNow - utc;
        return elapsed switch
        {
            { TotalSeconds: < 60 } => "hace un momento",
            { TotalMinutes: < 60 } => $"hace {(int)elapsed.TotalMinutes} min",
            { TotalHours: < 24 } => $"hace {(int)elapsed.TotalHours} h",
            { TotalDays: < 2 } => "ayer",
            { TotalDays: < 30 } => $"hace {(int)elapsed.TotalDays} días",
            _ => utc.ToString("d MMM yyyy", culture),
        };
    }
}
