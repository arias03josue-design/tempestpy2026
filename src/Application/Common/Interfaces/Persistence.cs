using Tempest.Domain.Entities;
using Tempest.Domain.ValueObjects;

namespace Tempest.Application.Common.Interfaces;

/// <summary>Commits the changes tracked by the repositories of the current request.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IProductRepository
{
    /// <summary>Tracked entity, ready to be modified.</summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Active products, newest first (read-only).</summary>
    Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>Every product, newest first (read-only).</summary>
    Task<IReadOnlyList<Product>> ListAllAsync(CancellationToken cancellationToken);

    Task<bool> IsImageInUseAsync(string imageUrl, CancellationToken cancellationToken);

    void Add(Product product);

    void Remove(Product product);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Username username, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Cheap projection used to validate sessions on every request.</summary>
    Task<string?> GetSecurityStampAsync(Guid id, CancellationToken cancellationToken);

    void Add(User user);
}

public interface IStoreSettingsRepository
{
    Task<StoreSettings?> GetAsync(CancellationToken cancellationToken);

    void Add(StoreSettings settings);
}
