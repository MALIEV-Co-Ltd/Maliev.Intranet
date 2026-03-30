namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Provides upload configuration including maximum file sizes and concurrency limits,
/// loaded from the Upload section of appsettings.json.
/// </summary>
public sealed class UploadSettings
{
    /// <summary>Maximum number of concurrent file uploads.</summary>
    public int MaxConcurrentUploads { get; init; } = 3;

    /// <summary>
    /// Maximum file size in bytes per category.
    /// Keys: "ThreeDModel", "Drawing", "Document".
    /// </summary>
    public Dictionary<string, long> MaxFileSizeBytes { get; init; } = new();

    /// <summary>Maximum file size for 3D model uploads (STL, STEP, 3MF, etc.). Defaults to 100 MB.</summary>
    public long ThreeDModelLimit => MaxFileSizeBytes.GetValueOrDefault("ThreeDModel", 104857600);

    /// <summary>Maximum file size for drawing uploads (PDF, DXF, DWG). Defaults to 50 MB.</summary>
    public long DrawingLimit => MaxFileSizeBytes.GetValueOrDefault("Drawing", 52428800);

    /// <summary>Maximum file size for document uploads. Defaults to 50 MB.</summary>
    public long DocumentLimit => MaxFileSizeBytes.GetValueOrDefault("Document", 52428800);
}
