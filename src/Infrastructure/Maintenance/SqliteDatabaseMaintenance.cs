using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tempest.Application.Common.Results;
using Tempest.Application.Maintenance;
using Tempest.Application.Maintenance.Commands;
using Tempest.Application.Products.Commands;
using Tempest.Infrastructure.Persistence;
using Tempest.Infrastructure.Storage;

namespace Tempest.Infrastructure.Maintenance;

/// <summary>
/// Backup format (.zip): <c>tempest.db</c> (consistent snapshot), <c>uploads/*</c> (uploaded images) and
/// <c>manifest.json</c>. Data Protection keys are not included: after a restore the admins sign in again.
/// </summary>
internal sealed class SqliteDatabaseMaintenance(
    AppDbContext db,
    IOptions<StorageOptions> storageOptions,
    TimeProvider timeProvider,
    ILogger<SqliteDatabaseMaintenance> logger)
    : IDatabaseMaintenance
{
    private const string DatabaseEntry = "tempest.db";
    private const string UploadsFolder = "uploads/";
    private const string ManifestEntry = "manifest.json";
    private const long MaxDatabaseBytes = 4L * 1024 * 1024 * 1024;
    private const string Field = nameof(RestoreBackupCommand.Archive);

    private static readonly string[] RequiredTables = ["Products", "Users", "StoreSettings", "__EFMigrationsHistory"];

    private string UploadsPath => storageOptions.Value.UploadsPath;

    public Task<StorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken)
    {
        var database = SqliteSnapshot.DatabasePath(db);
        var uploads = ManagedUploads().ToArray();

        // The WAL file holds recent writes until SQLite checkpoints them, so it is part of the real size.
        var databaseBytes = SizeOf(database) + SizeOf(database + "-wal");
        return Task.FromResult(new StorageInfo(databaseBytes, uploads.Sum(file => file.Length), uploads.Length));
    }

    public async Task<BackupArchive> CreateBackupArchiveAsync(CancellationToken cancellationToken)
    {
        var stamp = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMdd-HHmmss");
        var work = Directory.CreateTempSubdirectory("tempest-backup-");
        var archivePath = Path.Combine(Path.GetTempPath(), $"tempest-backup-{stamp}-{Guid.NewGuid():N}.zip");

        try
        {
            var snapshot = Path.Combine(work.FullName, DatabaseEntry);
            await SqliteSnapshot.CreateAsync(db, snapshot, cancellationToken);
            var migrations = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();

            await using (var stream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81_920, useAsync: true))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(snapshot, DatabaseEntry, CompressionLevel.Optimal);

                foreach (var image in ManagedUploads())
                {
                    // Images are already compressed: store them as-is.
                    zip.CreateEntryFromFile(image.FullName, UploadsFolder + image.Name, CompressionLevel.NoCompression);
                }

                var manifest = zip.CreateEntry(ManifestEntry);
                await using var writer = manifest.Open();
                await JsonSerializer.SerializeAsync(
                    writer,
                    new { format = 1, app = "tempest-store", createdAtUtc = timeProvider.GetUtcNow(), migration = migrations.LastOrDefault() },
                    cancellationToken: cancellationToken);
            }

            logger.LogInformation("Created backup archive {Archive}", archivePath);
            return new BackupArchive(archivePath, $"tempest-backup-{stamp}.zip", new FileInfo(archivePath).Length);
        }
        catch
        {
            File.Delete(archivePath);
            throw;
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }

    public async Task<Result<RestoreOutcome>> RestoreAsync(string archivePath, CancellationToken cancellationToken)
    {
        var work = Directory.CreateTempSubdirectory("tempest-restore-");
        try
        {
            ZipArchive zip;
            try
            {
                zip = ZipFile.OpenRead(archivePath);
            }
            catch (InvalidDataException)
            {
                return Invalid("El archivo no es un .zip válido.");
            }

            using (zip)
            {
                var databaseEntry = zip.GetEntry(DatabaseEntry);
                if (databaseEntry is null || databaseEntry.Length > MaxDatabaseBytes)
                {
                    return Invalid("El .zip no contiene una base de datos de la tienda (tempest.db).");
                }

                var candidate = Path.Combine(work.FullName, DatabaseEntry);
                databaseEntry.ExtractToFile(candidate);

                var inspection = await InspectAsync(candidate, cancellationToken);
                if (inspection.IsFailure)
                {
                    return inspection.Error;
                }

                // Safety net: the current data is kept next to the database before anything is replaced.
                var stamp = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMdd-HHmmss");
                var safetyCopy = $"before-restore-{stamp}.db";
                var databasePath = SqliteSnapshot.DatabasePath(db);
                await SqliteSnapshot.CreateAsync(db, Path.Combine(Path.GetDirectoryName(databasePath)!, "backups", safetyCopy), cancellationToken);

                // SQLite's online backup API rewrites the live database in place (open connections stay valid).
                await using (var source = SqliteSnapshot.Open(candidate))
                await using (var target = SqliteSnapshot.Open(databasePath))
                {
                    await source.OpenAsync(cancellationToken);
                    await target.OpenAsync(cancellationToken);
                    source.BackupDatabase(target);
                }

                SqliteConnection.ClearAllPools();

                // Backups taken with an older version get the newer migrations applied.
                await db.Database.MigrateAsync(cancellationToken);
                await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);

                var images = RestoreImages(zip);
                var (products, users) = inspection.Value;
                logger.LogWarning(
                    "Database restored from a backup ({Products} products, {Users} users, {Images} images). Safety copy: {SafetyCopy}",
                    products, users, images, safetyCopy);

                return new RestoreOutcome(products, users, images, safetyCopy);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            work.Delete(recursive: true);
        }
    }

    /// <summary>Accepts only an intact database of this store, created by this or an older version.</summary>
    private async Task<Result<(int Products, int Users)>> InspectAsync(string candidate, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.Equals(await SqliteSnapshot.IntegrityCheckAsync(candidate, cancellationToken), "ok", StringComparison.OrdinalIgnoreCase))
            {
                return Invalid("La base de datos del respaldo está dañada.");
            }

            await using var connection = SqliteSnapshot.Open(candidate);
            await connection.OpenAsync(cancellationToken);

            var tables = await QueryListAsync(connection, "SELECT name FROM sqlite_master WHERE type = 'table'", cancellationToken);
            if (RequiredTables.Any(table => !tables.Contains(table)))
            {
                return Invalid("El archivo no es un respaldo de esta tienda.");
            }

            var known = db.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
            var applied = await QueryListAsync(connection, "SELECT MigrationId FROM __EFMigrationsHistory", cancellationToken);
            if (applied.Any(migration => !known.Contains(migration)))
            {
                return Invalid("El respaldo fue creado con una versión más nueva de la tienda. Actualiza la aplicación primero.");
            }

            var users = await CountAsync(connection, "Users", cancellationToken);
            if (users == 0)
            {
                return Invalid("El respaldo no tiene usuarios: restaurarlo te dejaría sin acceso al panel.");
            }

            return (await CountAsync(connection, "Products", cancellationToken), users);
        }
        catch (SqliteException)
        {
            return Invalid("El archivo tempest.db del respaldo no es una base de datos SQLite válida.");
        }
    }

    /// <summary>Copies the backup images; current files are kept (unreferenced ones are harmless).</summary>
    private int RestoreImages(ZipArchive zip)
    {
        Directory.CreateDirectory(UploadsPath);
        var restored = 0;

        foreach (var entry in zip.Entries)
        {
            var isImage = entry.FullName == UploadsFolder + entry.Name
                          && UploadNames.Managed().IsMatch(entry.Name)
                          && entry.Length <= UploadProductImageCommand.MaxBytes;
            if (!isImage)
            {
                continue;
            }

            entry.ExtractToFile(Path.Combine(UploadsPath, entry.Name), overwrite: true);
            restored++;
        }

        return restored;
    }

    private IEnumerable<FileInfo> ManagedUploads()
    {
        var folder = new DirectoryInfo(UploadsPath);
        return folder.Exists
            ? folder.EnumerateFiles().Where(file => UploadNames.Managed().IsMatch(file.Name))
            : [];
    }

    private static long SizeOf(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

    private static Error Invalid(string message) => Error.Validation(Field, message);

    private static async Task<HashSet<string>> QueryListAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var values = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static async Task<int> CountAsync(SqliteConnection connection, string table, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{table}\"";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }
}
