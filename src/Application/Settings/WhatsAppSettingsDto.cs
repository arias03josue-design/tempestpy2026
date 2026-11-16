using Tempest.Domain.Entities;

namespace Tempest.Application.Settings;

/// <summary>WhatsApp parameters used to build every "Contactar por WhatsApp" link.</summary>
public sealed record WhatsAppSettingsDto(string? PhoneNumber, string MessageTemplate, DateTime? UpdatedAtUtc)
{
    public bool IsConfigured => !string.IsNullOrEmpty(PhoneNumber);
}

internal static class SettingsMappings
{
    public static WhatsAppSettingsDto ToDto(this StoreSettings settings) => new(
        settings.WhatsAppNumber?.Value,
        settings.MessageTemplate.Value,
        settings.UpdatedAtUtc);
}
