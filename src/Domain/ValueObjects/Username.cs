using System.Diagnostics.CodeAnalysis;
using Tempest.Domain.Common;

namespace Tempest.Domain.ValueObjects;

/// <summary>Login name. Normalized to lowercase so uniqueness is case-insensitive.</summary>
public sealed record Username
{
    public const int MinLength = 3;
    public const int MaxLength = 32;

    private Username(string value) => Value = value;

    public string Value { get; }

    public static Username Create(string? input) =>
        TryCreate(input, out var username)
            ? username
            : throw new DomainException(
                "user.invalid_username",
                $"El usuario debe tener entre {MinLength} y {MaxLength} caracteres: letras sin acentos, números, punto, guion o guion bajo.");

    public static bool TryCreate(string? input, [NotNullWhen(true)] out Username? username)
    {
        username = null;
        var value = input?.Trim().ToLowerInvariant();
        if (value is null || value.Length is < MinLength or > MaxLength)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '.' or '_' or '-'))
            {
                return false;
            }
        }

        username = new Username(value);
        return true;
    }

    public override string ToString() => Value;
}
