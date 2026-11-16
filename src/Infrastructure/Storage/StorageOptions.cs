namespace Tempest.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Folder for uploaded images. Relative paths are resolved against the content root.</summary>
    public string UploadsPath { get; set; } = "wwwroot/uploads";

    /// <summary>URL prefix under which the uploads are served.</summary>
    public string PublicBasePath { get; set; } = "/uploads";
}
