namespace Tempest.Application.Common.Interfaces;

/// <summary>Stores product images uploaded from the admin panel.</summary>
public interface IImageStorage
{
    /// <summary>Saves the file under a random name and returns its public URL (e.g. <c>/uploads/…webp</c>).</summary>
    Task<string> SaveAsync(ReadOnlyMemory<byte> content, string extension, CancellationToken cancellationToken);

    /// <summary>True when the URL points to a file managed by this storage (external URLs are not).</summary>
    bool IsManaged(string imageUrl);

    /// <summary>Deletes a managed file. Unknown or external URLs are ignored.</summary>
    Task DeleteAsync(string imageUrl, CancellationToken cancellationToken);
}
