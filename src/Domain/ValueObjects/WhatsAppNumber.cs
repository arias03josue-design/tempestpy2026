using System.Diagnostics.CodeAnalysis;
using Tempest.Domain.Common;

namespace Tempest.Domain.ValueObjects;

/// <summary>
/// WhatsApp phone number in international format: country code + number, digits only (no "+"),
/// as required by <c>https://wa.me/{number}</c>. Formatting characters people usually type
/// (spaces, dashes, dots, parentheses and a leading "+") are stripped.
/// </summary>
public sealed record WhatsAppNumber
{
    public const int MinDigits = 8;
    public const int MaxDigits = 15; // E.164 upper bound

    private WhatsAppNumber(string value) => Value = value;

    public string Value { get; }

    public static WhatsAppNumber Create(string? input) =>
        TryCreate(input, out var number)
            ? number
            : throw new DomainException(
                "whatsapp.invalid_number",
                $"El número de WhatsApp debe incluir el código de país, tener entre {MinDigits} y {MaxDigits} dígitos y no empezar con 0.");

    public static bool TryCreate(string? input, [NotNullWhen(true)] out WhatsAppNumber? number)
    {
        number = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        Span<char> digits = stackalloc char[MaxDigits];
        var count = 0;

        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (char.IsAsciiDigit(c))
            {
                if (count == MaxDigits)
                {
                    return false;
                }

                digits[count++] = c;
            }
            else if (c is ' ' or '-' or '.' or '(' or ')' || (c == '+' && i == 0))
            {
                // Formatting character: ignore.
            }
            else
            {
                return false;
            }
        }

        // Country codes never start with 0 (a leading 0 is a local trunk prefix).
        if (count < MinDigits || digits[0] == '0')
        {
            return false;
        }

        number = new WhatsAppNumber(new string(digits[..count]));
        return true;
    }

    public override string ToString() => Value;
}
