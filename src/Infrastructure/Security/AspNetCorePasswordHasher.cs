using Microsoft.AspNetCore.Identity;
using Tempest.Application.Common.Interfaces;

namespace Tempest.Infrastructure.Security;

/// <summary>
/// ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/>: PBKDF2-HMAC-SHA512 with a random
/// 128-bit salt and 100,000 iterations (format V3). It flags outdated hashes for transparent upgrade.
/// </summary>
internal sealed class AspNetCorePasswordHasher : IPasswordHasher
{
    // The default implementation does not use the user instance; a shared marker object is enough.
    private static readonly object HashOwner = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HashOwner, password);

    public PasswordVerification Verify(string passwordHash, string password)
    {
        try
        {
            return _hasher.VerifyHashedPassword(HashOwner, passwordHash, password) switch
            {
                PasswordVerificationResult.Success => PasswordVerification.Success,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
                _ => PasswordVerification.Failed,
            };
        }
        catch (FormatException)
        {
            // Corrupted hash in the database: treat as a failed login instead of crashing.
            return PasswordVerification.Failed;
        }
    }
}
