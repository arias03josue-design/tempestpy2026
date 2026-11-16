using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tempest.Domain.Entities;
using Tempest.Domain.ValueObjects;

namespace Tempest.Infrastructure.Persistence.Configurations;

internal sealed class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
{
    public void Configure(EntityTypeBuilder<StoreSettings> builder)
    {
        builder.ToTable("StoreSettings");
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).ValueGeneratedNever();

        builder.Property(settings => settings.WhatsAppNumber)
            .HasConversion(number => number!.Value, value => WhatsAppNumber.Create(value))
            .HasMaxLength(WhatsAppNumber.MaxDigits);

        builder.Property(settings => settings.MessageTemplate)
            .HasConversion(template => template.Value, value => MessageTemplate.Create(value))
            .HasMaxLength(MessageTemplate.MaxLength)
            .IsRequired();

        builder.Ignore(settings => settings.IsWhatsAppConfigured);
    }
}
