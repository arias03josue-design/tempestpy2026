using System.IO.Compression;
using Tempest.Application.Common.Results;
using Tempest.Application.Maintenance.Commands;
using Tempest.Application.Maintenance.Queries;
using Tempest.Application.Products.Commands;
using Tempest.Application.Products.Queries;
using Tempest.Domain.Enums;

namespace Tempest.Tests.Integration;

public sealed class RestoreTests
{
    [Fact]
    public async Task Exported_backup_restores_products_and_images_and_keeps_a_safety_copy()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SignInAsync();

        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        var image = (await app.SendAsync(new UploadProductImageCommand(new MemoryStream(png), "a.png"))).Value;
        var product = (await app.SendAsync(new CreateProductCommand("Antes del respaldo", null, 10m, image, Rarity.Rare, true))).Value;

        var backup = await app.SendAsync(new CreateBackupCommand());
        Assert.True(backup.IsSuccess, backup.Error?.Message);
        Assert.EndsWith(".zip", backup.Value.FileName);

        // Lose data after the backup: the product and its image are deleted.
        await app.SendAsync(new DeleteProductCommand(product));
        var imagePath = Path.Combine(app.UploadsPath, Path.GetFileName(image));
        Assert.False(File.Exists(imagePath));

        await using var archive = File.OpenRead(backup.Value.FilePath);
        var restored = await app.SendAsync(new RestoreBackupCommand(archive, backup.Value.FileName));

        Assert.True(restored.IsSuccess, restored.Error?.Message);
        Assert.Equal(1, restored.Value.RestoredImages);
        Assert.True(File.Exists(imagePath));
        Assert.True(File.Exists(Path.Combine(app.Root, "data", "backups", restored.Value.SafetyCopyFileName)));
        Assert.Contains((await app.SendAsync(new GetAdminProductsQuery())).Value, p => p.Name == "Antes del respaldo");

        var info = await app.SendAsync(new GetDatabaseInfoQuery());
        Assert.True(info.Value.DatabaseBytes > 0);
        Assert.Equal(1, info.Value.UploadFiles);
    }

    [Fact]
    public async Task Rejects_files_that_are_not_store_backups()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SignInAsync();

        var notZip = await app.SendAsync(new RestoreBackupCommand(new MemoryStream("hola"u8.ToArray()), "x.zip"));
        var fakeDatabase = await app.SendAsync(new RestoreBackupCommand(ZipWith("tempest.db", "no soy sqlite"), "x.zip"));
        var missingDatabase = await app.SendAsync(new RestoreBackupCommand(ZipWith("otra-cosa.txt", "hola"), "x.zip"));

        Assert.Equal(ErrorType.Validation, notZip.Error?.Type);
        Assert.Equal(ErrorType.Validation, fakeDatabase.Error?.Type);
        Assert.Equal(ErrorType.Validation, missingDatabase.Error?.Type);
        Assert.Equal(8, (await app.SendAsync(new GetPublicCatalogQuery())).Value.Count); // data untouched
    }

    [Fact]
    public async Task Backup_requires_an_authenticated_admin()
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.SendAsync(new CreateBackupCommand());

        Assert.Equal(ErrorType.Unauthorized, result.Error?.Type);
    }

    private static MemoryStream ZipWith(string entryName, string content)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(zip.CreateEntry(entryName).Open());
            writer.Write(content);
        }

        stream.Position = 0;
        return stream;
    }
}
