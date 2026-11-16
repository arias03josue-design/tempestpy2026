using System.Security.Cryptography;
using Tempest.Domain.Common;
using Tempest.Domain.ValueObjects;

namespace Tempest.Domain.Entities;

/// <summary>Administrator account (every user can manage the store).</summary>
public sealed class User : Entity<Guid>
{
    private User()
    {
        // Required by EF Core.
    }

    public Username Username { get; private set; } = null!;

    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Random value embedded in the auth cookie. Rotating it invalidates every existing session
    /// (used when the password changes).
    /// </summary>
    public string SecurityStamp { get; private set; } = string.Empty;

    /// <summary>True for seeded/created accounts whose password was chosen by someone else.</summary>
    public bool MustChangePassword { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public DateTime? PasswordChangedAtUtc { get; private set; }

    public static User Create(Username username, string passwordHash, bool mustChangePassword, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(username);
        EnsureHash(passwordHash);

        return new User
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            PasswordHash = passwordHash,
            SecurityStamp = NewSecurityStamp(),
            MustChangePassword = mustChangePassword,
            CreatedAtUtc = utcNow,
        };
    }

    /// <summary>Sets a new password and rotates the security stamp, signing out every other session.</summary>
    public void ChangePassword(string newPasswordHash, DateTime utcNow)
    {
        EnsureHash(newPasswordHash);
        PasswordHash = newPasswordHash;
        SecurityStamp = NewSecurityStamp();
        MustChangePassword = false;
        PasswordChangedAtUtc = utcNow;
    }

    /// <summary>Stores a stronger hash of the same password (no impact on existing sessions).</summary>
    public void UpgradePasswordHash(string passwordHash)
    {
        EnsureHash(passwordHash);
        PasswordHash = passwordHash;
    }

    public void RecordLogin(DateTime utcNow) => LastLoginAtUtc = utcNow;

    private static void EnsureHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("user.invalid_password_hash", "El hash de la contraseña es obligatorio.");
        }
    }

    private static string NewSecurityStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}
