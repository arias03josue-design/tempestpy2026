using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tempest.Application.Common.Interfaces;
using Tempest.Domain.Entities;
using Tempest.Domain.ValueObjects;
using Tempest.Infrastructure.Persistence.Seed;

namespace Tempest.Infrastructure.Persistence;

/// <summary>Applies migrations and seeds first-run data. Safe to run on every start.</summary>
internal sealed class DatabaseInitializer(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<SeedOptions> seedOptions,
    TimeProvider timeProvider,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        EnsureDatabaseDirectory();

        await db.Database.MigrateAsync(cancellationToken);

        // WAL lets the public catalog keep reading while an admin writes. The mode is persisted in the file.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);

        var seed = seedOptions.Value;
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            var admin = User.Create(
                Username.Create(seed.AdminUsername),
                passwordHasher.Hash(seed.AdminPassword),
                mustChangePassword: true,
                utcNow);

            db.Users.Add(admin);
            logger.LogWarning(
                "No users found: created the initial administrator '{Username}'. Change its password from /admin.",
                admin.Username.Value);
        }

        // StoreSettings only exists after the first run, so deleting every product never re-seeds the demo.
        if (!await db.StoreSettings.AnyAsync(cancellationToken))
        {
            var whatsApp = WhatsAppNumber.TryCreate(seed.WhatsAppNumber, out var number) ? number : null;
            db.StoreSettings.Add(StoreSettings.CreateDefault(whatsApp));

            if (seed.SampleProducts && !await db.Products.AnyAsync(cancellationToken))
            {
                db.Products.AddRange(SampleCatalog.Create(utcNow));
                logger.LogInformation("Seeded the sample catalog.");
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private void EnsureDatabaseDirectory()
    {
        var dataSource = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        var directory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
