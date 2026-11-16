using System.Globalization;

namespace Tempest.WebUI.Shared.Formatting;

public static class ByteSize
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    /// <summary>Human-readable size: 45 KB, 12.4 MB, 1.25 GB.</summary>
    public static string Format(long bytes, CultureInfo culture)
    {
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes} B"
            : $"{value.ToString(value < 10 ? "0.##" : "0.#", culture)} {Units[unit]}";
    }

    /// <summary>Exact value in GB (1 GB = 1024³ bytes), with enough decimals for small databases.</summary>
    public static string ToGigabytes(long bytes, CultureInfo culture) =>
        $"{(bytes / 1_073_741_824d).ToString("0.0000", culture)} GB";
}
