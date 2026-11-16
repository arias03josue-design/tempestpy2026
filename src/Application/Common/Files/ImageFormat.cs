namespace Tempest.Application.Common.Files;

/// <summary>
/// Image formats accepted for uploads. The format is detected from the file signature ("magic bytes"),
/// never from the client-provided name or content type. SVG is intentionally excluded (it can embed scripts).
/// </summary>
public sealed record ImageFormat(string Extension, string ContentType)
{
    public static readonly ImageFormat Jpeg = new(".jpg", "image/jpeg");
    public static readonly ImageFormat Png = new(".png", "image/png");
    public static readonly ImageFormat Gif = new(".gif", "image/gif");
    public static readonly ImageFormat WebP = new(".webp", "image/webp");

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static ImageFormat? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return Jpeg;
        }

        if (header.StartsWith(PngSignature))
        {
            return Png;
        }

        if (header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8))
        {
            return Gif;
        }

        if (header.Length >= 12 && header.StartsWith("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return WebP;
        }

        return null;
    }
}
