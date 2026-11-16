namespace Tempest.Infrastructure.Persistence.Seed;

/// <summary>First-run data. Override with environment variables, e.g. <c>Seed__AdminPassword</c>.</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Created only when the Users table is empty.</summary>
    public string AdminUsername { get; set; } = "admin";

    public string AdminPassword { get; set; } = "admin";

    /// <summary>Optional initial WhatsApp number (digits with country code).</summary>
    public string? WhatsAppNumber { get; set; }

    /// <summary>Adds the demo catalog on the very first run.</summary>
    public bool SampleProducts { get; set; } = true;
}
