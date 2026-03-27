namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// A single directional preview image stored by OrderService for a given order's primary 3D file.
/// The StoragePath is a raw GCS object path — the BFF resolves it to a signed URL before returning to clients.
/// </summary>
public class OrderPreviewImageDto
{
    /// <summary>Gets or sets the view direction (Front, Back, Left, Right, Top, Bottom).</summary>
    public string Side { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw GCS storage path for this preview image.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Gets or sets when this preview was generated.</summary>
    public DateTime GeneratedAt { get; set; }
}
