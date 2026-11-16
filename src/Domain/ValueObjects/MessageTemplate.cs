using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Tempest.Domain.Common;

namespace Tempest.Domain.ValueObjects;

/// <summary>
/// Pre-filled WhatsApp message. Supports the <c>{producto}</c> and <c>{precio}</c> placeholders
/// (case-insensitive), replaced with the data of the selected catalog card.
/// </summary>
public sealed record MessageTemplate
{
    public const int MaxLength = 1000;
    public const string ProductPlaceholder = "{producto}";
    public const string PricePlaceholder = "{precio}";
    public const string DefaultText = "¡Hola! Me interesa comprar el producto {producto} que vi a {precio}.";

    private MessageTemplate(string value) => Value = value;

    public static MessageTemplate Default { get; } = new(DefaultText);

    public string Value { get; }

    public static MessageTemplate Create(string? input) =>
        TryCreate(input, out var template)
            ? template
            : throw new DomainException(
                "whatsapp.invalid_template",
                $"El mensaje es obligatorio y admite hasta {MaxLength} caracteres.");

    public static bool TryCreate(string? input, [NotNullWhen(true)] out MessageTemplate? template)
    {
        template = null;
        var value = input?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
        {
            return false;
        }

        template = new MessageTemplate(value);
        return true;
    }

    /// <summary>
    /// Replaces the placeholders in a single pass, so a product name that happens to contain
    /// "{precio}" is never re-interpreted.
    /// </summary>
    public string Render(string productName, string formattedPrice) =>
        MessagePlaceholders.Pattern().Replace(Value, match =>
            match.Groups[1].Value.Equals("producto", StringComparison.OrdinalIgnoreCase)
                ? productName
                : formattedPrice);

    public bool Contains(string placeholder) => Value.Contains(placeholder, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;
}

internal static partial class MessagePlaceholders
{
    [GeneratedRegex(@"\{(producto|precio)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    internal static partial Regex Pattern();
}
