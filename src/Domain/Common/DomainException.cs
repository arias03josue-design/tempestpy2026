namespace Tempest.Domain.Common;

/// <summary>Raised when an operation would break a domain invariant.</summary>
/// <param name="code">Stable, machine-readable identifier (e.g. <c>product.invalid_price</c>).</param>
/// <param name="message">Human-readable explanation shown to the admin.</param>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
