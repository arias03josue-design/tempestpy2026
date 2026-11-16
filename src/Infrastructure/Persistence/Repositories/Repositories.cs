using Microsoft.EntityFrameworkCore;
using Tempest.Application.Common.Interfaces;
using Tempest.Domain.Entities;
using Tempest.Domain.ValueObjects;

namespace Tempest.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Products.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListActiveAsync(CancellationToken cancellationToken) =>
        await db.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderByDescending(product => product.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAllAsync(CancellationToken cancellationToken) =>
        await db.Products
            .AsNoTracking()
            .OrderByDescending(product => product.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> IsImageInUseAsync(string imageUrl, CancellationToken cancellationToken) =>
        db.Products.AnyAsync(product => product.ImageUrl == imageUrl, cancellationToken);

    public void Add(Product product) => db.Products.Add(product);

    public void Remove(Product product) => db.Products.Remove(product);
}

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.Username == username, cancellationToken);

    public Task<bool> ExistsAsync(Username username, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(user => user.Username == username, cancellationToken);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken) =>
        await db.Users
            .AsNoTracking()
            .OrderBy(user => user.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<string?> GetSecurityStampAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => (string?)user.SecurityStamp)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(User user) => db.Users.Add(user);
}

internal sealed class StoreSettingsRepository(AppDbContext db) : IStoreSettingsRepository
{
    public Task<StoreSettings?> GetAsync(CancellationToken cancellationToken) =>
        db.StoreSettings.FirstOrDefaultAsync(settings => settings.Id == StoreSettings.SingletonId, cancellationToken);

    public void Add(StoreSettings settings) => db.StoreSettings.Add(settings);
}
