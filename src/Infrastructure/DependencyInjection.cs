using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Maintenance;
using Tempest.Infrastructure.Health;
using Tempest.Infrastructure.Maintenance;
using Tempest.Infrastructure.Persistence;
using Tempest.Infrastructure.Persistence.Repositories;
using Tempest.Infrastructure.Persistence.Seed;
using Tempest.Infrastructure.Security;
using Tempest.Infrastructure.Storage;

namespace Tempest.Infrastructure;

public static class DependencyInjection
{
    private const string DefaultConnectionString = "Data Source=data/tempest.db";

    /// <param name="contentRootPath">Base for relative paths (database file, uploads folder).</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var connectionString = ResolveConnectionString(configuration.GetConnectionString("Default"), contentRootPath);

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStoreSettingsRepository, StoreSettingsRepository>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<IDatabaseMaintenance, SqliteDatabaseMaintenance>();

        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
        services.AddSingleton<IImageStorage, LocalImageStorage>();

        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName));
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .PostConfigure(options =>
            {
                options.UploadsPath = Path.GetFullPath(options.UploadsPath, contentRootPath);
                options.PublicBasePath = "/" + options.PublicBasePath.Trim('/');
            });

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        return services;
    }

    /// <summary>Applies migrations and seeds the first admin (admin/admin) plus the sample catalog.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Writes a consistent snapshot of the SQLite database with <c>VACUUM INTO</c>. Safe while the store is
    /// running: it reads a single transaction, including changes still in the WAL file (a plain file copy of
    /// <c>tempest.db</c> would miss them). The copy is integrity-checked; existing files are never overwritten.
    /// </summary>
    /// <returns>The full path of the written backup.</returns>
    public static async Task<string> BackupDatabaseAsync(
        this IServiceProvider services,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var target = Path.GetFullPath(destinationPath);
        await using var scope = services.CreateAsyncScope();
        await SqliteSnapshot.CreateAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), target, cancellationToken);
        return target;
    }

    /// <summary>SQLite resolves relative paths against the process working directory; anchor them to the content root.</summary>
    private static string ResolveConnectionString(string? configured, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(
            string.IsNullOrWhiteSpace(configured) ? DefaultConnectionString : configured);

        var isFile = builder.Mode != SqliteOpenMode.Memory
                     && !string.IsNullOrEmpty(builder.DataSource)
                     && builder.DataSource != ":memory:"
                     && !builder.DataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase);

        if (isFile)
        {
            builder.DataSource = Path.GetFullPath(builder.DataSource, contentRootPath);
        }

        return builder.ToString();
    }
}
