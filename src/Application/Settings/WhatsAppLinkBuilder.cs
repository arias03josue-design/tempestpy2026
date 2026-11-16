using Tempest.Domain.ValueObjects;

namespace Tempest.Application.Settings;

/// <summary>Builds WhatsApp click-to-chat links: <c>https://wa.me/{numero}?text={mensaje_codificado}</c>.</summary>
public static class WhatsAppLinkBuilder
{
    public const string ProductPlaceholder = MessageTemplate.ProductPlaceholder;
    public const string PricePlaceholder = MessageTemplate.PricePlaceholder;

    private const string BaseUrl = "https://wa.me/";

    /// <summary>Link for a catalog card, or <c>null</c> while the store has not configured its number.</summary>
    public static string? ForProduct(WhatsAppSettingsDto settings, string productName, string formattedPrice)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.IsConfigured
            ? Build(settings.PhoneNumber!, RenderMessage(settings.MessageTemplate, productName, formattedPrice))
            : null;
    }

    /// <summary>Replaces <c>{producto}</c> and <c>{precio}</c>; falls back to the default template when empty.</summary>
    public static string RenderMessage(string? template, string productName, string formattedPrice)
    {
        var messageTemplate = MessageTemplate.TryCreate(template, out var parsed) ? parsed : MessageTemplate.Default;
        return messageTemplate.Render(productName, formattedPrice);
    }

    public static string Build(string phoneNumber, string message) =>
        $"{BaseUrl}{phoneNumber}?text={Uri.EscapeDataString(message)}";

    /// <summary>"+52 (55) 1234-5678" → "525512345678"; <c>null</c> when the input is not a valid number.</summary>
    public static string? NormalizePhoneNumber(string? input) =>
        WhatsAppNumber.TryCreate(input, out var number) ? number.Value : null;
}
