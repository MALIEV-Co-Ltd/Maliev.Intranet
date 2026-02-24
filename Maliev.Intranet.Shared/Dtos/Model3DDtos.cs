namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for a 3D model file.
/// </summary>
public sealed record Model3DDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the internal file name.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Gets or sets the original file name as uploaded.</summary>
    public string? OriginalFileName { get; set; }
    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSize { get; set; }
    /// <summary>Gets or sets the MIME content type.</summary>
    public string ContentType { get; set; } = string.Empty;
    /// <summary>Gets or sets the upload timestamp.</summary>
    public DateTime UploadedAt { get; set; }
    /// <summary>Gets or sets the identifier of the person who uploaded the file.</summary>
    public string? UploadedBy { get; set; }

    // Geometry Analysis
    /// <summary>Gets or sets a value indicating whether the geometry has been analyzed.</summary>
    public bool GeometryAnalyzed { get; set; }
    /// <summary>Gets or sets the part volume in cubic centimeters.</summary>
    public double? VolumeCm3 { get; set; }
    /// <summary>Gets or sets the estimated support volume in cubic centimeters.</summary>
    public double? SupportVolumeCm3 { get; set; }
    /// <summary>Gets or sets the surface area in square centimeters.</summary>
    public double? SurfaceAreaCm2 { get; set; }
    /// <summary>Gets or sets the bounding box X dimension in mm.</summary>
    public double? BoundingBoxX { get; set; }
    /// <summary>Gets or sets the bounding box Y dimension in mm.</summary>
    public double? BoundingBoxY { get; set; }
    /// <summary>Gets or sets the bounding box Z dimension in mm.</summary>
    public double? BoundingBoxZ { get; set; }
    /// <summary>Gets or sets a value indicating whether the mesh is manifold.</summary>
    public bool? IsManifold { get; set; }
    /// <summary>Gets or sets the triangle count of the mesh.</summary>
    public int? TriangleCount { get; set; }

    // Viewer Artifacts (Signed URLs)
    /// <summary>Gets or sets the URL for the 3D viewer.</summary>
    public string? ViewerUrl { get; set; }
    /// <summary>Gets or sets the URL for the file thumbnail.</summary>
    public string? ThumbnailUrl { get; set; }
    /// <summary>Gets or sets the signed URL for downloading the file.</summary>
    public string? DownloadUrl { get; set; }
}
