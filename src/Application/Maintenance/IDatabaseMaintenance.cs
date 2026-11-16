using Tempest.Application.Common.Results;

namespace Tempest.Application.Maintenance;

/// <summary>Disk usage of the store data.</summary>
public sealed record StorageInfo(long DatabaseBytes, long UploadsBytes, int UploadFiles);

/// <summary>A backup .zip written to a temporary file, ready to be streamed to the admin.</summary>
public sealed record BackupArchive(string FilePath, string FileName, long Bytes);

/// <summary>What a restore brought back, plus the safety copy taken just before replacing the data.</summary>
public sealed record RestoreOutcome(int Products, int Users, int RestoredImages, string SafetyCopyFileName);

/// <summary>Port implemented by Infrastructure (SQLite + local uploads folder).</summary>
public interface IDatabaseMaintenance
{
    Task<StorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken);

    /// <summary>Consistent snapshot of the live database plus the uploaded images, as a .zip.</summary>
    Task<BackupArchive> CreateBackupArchiveAsync(CancellationToken cancellationToken);

    /// <summary>Validates a backup .zip and replaces the current data with it.</summary>
    Task<Result<RestoreOutcome>> RestoreAsync(string archivePath, CancellationToken cancellationToken);
}
