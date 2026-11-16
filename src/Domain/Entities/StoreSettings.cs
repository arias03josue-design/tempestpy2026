using Tempest.Domain.Common;
using Tempest.Domain.ValueObjects;

namespace Tempest.Domain.Entities;

/// <summary>Store-wide configuration (singleton row) editable from the admin panel.</summary>
public sealed class StoreSettings : Entity<int>
{
    public const int SingletonId = 1;

    private StoreSettings()
    {
        // Required by EF Core.
    }

    /// <summary>Sales number used by every "Contactar por WhatsApp" button. Null until configured.</summary>
    public WhatsAppNumber? WhatsAppNumber { get; private set; }

    public MessageTemplate MessageTemplate { get; private set; } = MessageTemplate.Default;

    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsWhatsAppConfigured => WhatsAppNumber is not null;

    public static StoreSettings CreateDefault(WhatsAppNumber? whatsAppNumber = null) => new()
    {
        Id = SingletonId,
        WhatsAppNumber = whatsAppNumber,
        MessageTemplate = MessageTemplate.Default,
    };

    public void UpdateWhatsApp(WhatsAppNumber number, MessageTemplate template, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(template);

        WhatsAppNumber = number;
        MessageTemplate = template;
        UpdatedAtUtc = utcNow;
    }
}
