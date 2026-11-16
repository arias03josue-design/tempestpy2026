using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tempest.Application.Common.Interfaces;

namespace Tempest.Infrastructure.Storage;

/// <summary>
/// Saves uploads to the local disk (a Docker volume in production) under random names.
/// Only names generated here are ever deleted, which rules out path traversal.
/// </summary>
internal sealed class LocalImageStorage(IOptions<StorageOptions> options, ILogger<LocalImageStorage> logger)
    : IImageStorage
{
    private readonly string _root = options.Value.UploadsPath;
    private readonly string _publicBase = options.Value.PublicBasePath.TrimEnd('/');

    public async Task<string> SaveAsync(ReadOnlyMemory<byte> content, string extension, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);

        var fileName = $"{Guid.CreateVersion7():N}{extension}";
        var path = Path.Combine(_root, fileName);

        await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81_920, useAsync: true))
        {
            await stream.WriteAsync(content, cancellationToken);
        }

        logger.LogInformation("Stored uploaded image {FileName} ({Bytes} bytes)", fileName, content.Length);
        return $"{_publicBase}/{fileName}";
    }

    public bool IsManaged(string imageUrl) => TryGetFileName(imageUrl, out _);

    public Task DeleteAsync(string imageUrl, CancellationToken cancellationToken)
    {
        if (!TryGetFileName(imageUrl, out var fileName))
        {
            return Task.CompletedTask;
        }

        try
        {
            File.Delete(Path.Combine(_root, fileName));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not delete uploaded image {FileName}", fileName);
        }

        return Task.CompletedTask;
    }

    private bool TryGetFileName(string? imageUrl, out string fileName)
    {
        fileName = string.Empty;
        var prefix = _publicBase + "/";
        if (imageUrl is null || !imageUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var candidate = imageUrl[prefix.Length..];
        if (!UploadNames.Managed().IsMatch(candidate))
        {
            return false;
        }

        fileName = candidate;
        return true;
    }
}
