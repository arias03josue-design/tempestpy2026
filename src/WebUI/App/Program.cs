using Tempest.Application;
using Tempest.Infrastructure;
using Tempest.WebUI.App;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath)
    .AddWebUI(builder.Configuration, builder.Environment);

var app = builder.Build();

// Maintenance command used by deploy/backup.sh through `docker exec` (no web server is started):
//   dotnet Tempest.WebUI.dll --backup /tmp/tempest.db
if (app.Configuration["backup"] is { Length: > 0 } backupPath)
{
    try
    {
        var written = await app.Services.BackupDatabaseAsync(backupPath);
        Console.WriteLine($"Backup written to {written}");
    }
    catch (Exception ex)
    {
        // One clear line for cron logs; a non-zero exit code makes backup.sh stop.
        Console.Error.WriteLine($"Backup failed: {ex.Message}");
        Environment.ExitCode = 1;
    }

    await app.DisposeAsync();
    return;
}

// EF Core migrations + WAL + first-run seed (admin/admin and the sample catalog).
await app.Services.InitializeDatabaseAsync();

app.UseWebUI();

await app.RunAsync();
