using Microsoft.Data.Sqlite;
using Tempest.Application.Products.Commands;
using Tempest.Domain.Enums;

namespace Tempest.Tests.Integration;

public sealed class BackupTests
{
    [Fact]
    public async Task Online_backup_includes_changes_still_in_the_wal_file()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SignInAsync();
        await app.SendAsync(new CreateProductCommand("Producto respaldado", null, 10m, "/x.png", Rarity.Common, true));
        var target = Path.Combine(app.Root, "backups", "tempest.db");

        var written = await app.BackupAsync(target);

        Assert.Equal(Path.GetFullPath(target), written);
        Assert.Equal(1L, await CountAsync(target, "SELECT COUNT(*) FROM Products WHERE Name = 'Producto respaldado'"));
        Assert.Equal(1L, await CountAsync(target, "SELECT COUNT(*) FROM Users"));
    }

    [Fact]
    public async Task Never_overwrites_an_existing_backup()
    {
        await using var app = await TestApp.CreateAsync();
        var target = Path.Combine(app.Root, "backups", "tempest.db");
        await app.BackupAsync(target);

        await Assert.ThrowsAsync<IOException>(() => app.BackupAsync(target));
    }

    private static async Task<long> CountAsync(string databasePath, string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
