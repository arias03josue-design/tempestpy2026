namespace Tempest.Domain.Common;

/// <summary>Base type for domain entities identified by a key.</summary>
public abstract class Entity<TId>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;
}
