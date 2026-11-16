using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tempest.Infrastructure.Persistence;

/// <summary>Consistent copies of the live SQLite database.</summary>
internal static class SqliteSnapshot
{
    public static string DatabasePath(AppDbContext db) =>
        new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;

    /// <summary>
    /// <c>VACUUM INTO</c>: reads a single transaction (WAL included, safe while the store is running) and writes
    /// a compact copy, which is then integrity-checked. Existing files are never overwritten.
    /// </summary>
    public static async Task CreateAsync(AppDbContext db, string target, CancellationToken cancellationToken)
    {
        if (File.Exists(target))
        {
            throw new IOException($"The backup file already exists: {target}");
        }

        var source = DatabasePath(db);
        if (!File.Exists(source))
        {
            throw new FileNotFoundException("The database does not exist yet: start the store once first.", source);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await db.Database.ExecuteSqlAsync($"VACUUM INTO {target}", cancellationToken);

        var result = await IntegrityCheckAsync(target, cancellationToken);
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"The backup failed SQLite's integrity check: {result}");
        }
    }

    public static async Task<string?> IntegrityCheckAsync(string path, CancellationToken cancellationToken)
    {
        await using var connection = Open(path);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
    }

    /// <summary>Unpooled connection, so the file is released as soon as it is disposed.</summary>
    public static SqliteConnection Open(string path) =>
        new(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
}
