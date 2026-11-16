using FluentValidation;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;

namespace Tempest.Application.Maintenance.Commands;

/// <summary>Builds a downloadable backup (.zip with the database and the uploaded images).</summary>
public sealed record CreateBackupCommand : ICommand<BackupArchive>;

internal sealed class CreateBackupCommandHandler(IDatabaseMaintenance maintenance)
    : ICommandHandler<CreateBackupCommand, BackupArchive>
{
    public async Task<Result<BackupArchive>> Handle(CreateBackupCommand request, CancellationToken cancellationToken) =>
        await maintenance.CreateBackupArchiveAsync(cancellationToken);
}

/// <summary>Replaces products, users, settings and images with the content of a backup .zip.</summary>
public sealed record RestoreBackupCommand(Stream Archive, string FileName) : ICommand<RestoreOutcome>
{
    public const long MaxBytes = 1024L * 1024 * 1024; // 1 GB
}

internal sealed class RestoreBackupCommandValidator : AbstractValidator<RestoreBackupCommand>
{
    public RestoreBackupCommandValidator()
    {
        RuleFor(x => x.Archive).NotNull().Must(stream => stream.CanRead).WithMessage("No se pudo leer el archivo.");
        RuleFor(x => x.FileName)
            .Must(name => name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
            .WithMessage("Selecciona un respaldo .zip descargado desde este panel.");
    }
}

internal sealed class RestoreBackupCommandHandler(IDatabaseMaintenance maintenance)
    : ICommandHandler<RestoreBackupCommand, RestoreOutcome>
{
    private const string Field = nameof(RestoreBackupCommand.Archive);

    public async Task<Result<RestoreOutcome>> Handle(RestoreBackupCommand request, CancellationToken cancellationToken)
    {
        var temporary = Path.Combine(Path.GetTempPath(), $"tempest-restore-{Guid.NewGuid():N}.zip");
        try
        {
            // Spool to disk with a hard limit (never trust the size reported by the browser).
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81_920, useAsync: true))
            {
                var buffer = new byte[81_920];
                long total = 0;
                int read;
                while ((read = await request.Archive.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
                {
                    total += read;
                    if (total > RestoreBackupCommand.MaxBytes)
                    {
                        return Error.Validation(Field, "El respaldo supera el máximo de 1 GB.");
                    }

                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }

                if (total == 0)
                {
                    return Error.Validation(Field, "El archivo está vacío.");
                }
            }

            return await maintenance.RestoreAsync(temporary, cancellationToken);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
