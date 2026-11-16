using FluentValidation;
using Tempest.Application.Common.Results;
using Tempest.Domain.Entities;

namespace Tempest.Application.Users;

public sealed record UserDto(
    Guid Id,
    string Username,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    bool MustChangePassword);

/// <summary>Data the presentation layer needs to issue the session cookie.</summary>
public sealed record AuthenticatedUserDto(Guid Id, string Username, string SecurityStamp, bool MustChangePassword);

public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    internal static IRuleBuilderOptions<T, string> ValidNewPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Ingresa una contraseña.")
            .MinimumLength(MinLength).WithMessage($"La contraseña debe tener al menos {MinLength} caracteres.")
            .MaximumLength(MaxLength).WithMessage($"La contraseña admite hasta {MaxLength} caracteres.");
}

internal static class UserErrors
{
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("auth.invalid_credentials", "Usuario o contraseña incorrectos.");

    public static readonly Error NotFound =
        Error.NotFound("user.not_found", "La cuenta ya no existe.");
}

internal static class UserMappings
{
    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.Username.Value,
        user.CreatedAtUtc,
        user.LastLoginAtUtc,
        user.MustChangePassword);
}
