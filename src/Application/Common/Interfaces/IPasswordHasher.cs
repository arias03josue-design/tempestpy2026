namespace Tempest.Application.Common.Interfaces;

public enum PasswordVerification
{
    Failed,
    Success,

    /// <summary>Valid password, but the hash uses outdated parameters and should be upgraded.</summary>
    SuccessRehashNeeded,
}

/// <summary>Slow, salted password hashing (implemented in Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerification Verify(string passwordHash, string password);
}
