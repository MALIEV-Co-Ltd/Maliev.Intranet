namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// DTO for a 3D model file.
/// </summary>
public sealed record Model3DDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }

    // Geometry Analysis
    public bool GeometryAnalyzed { get; set; }
    public double? VolumeCm3 { get; set; }
    public double? SupportVolumeCm3 { get; set; }
    public double? SurfaceAreaCm2 { get; set; }
    public double? BoundingBoxX { get; set; }
    public double? BoundingBoxY { get; set; }
    public double? BoundingBoxZ { get; set; }
    public bool? IsManifold { get; set; }
    public int? TriangleCount { get; set; }

    // Viewer Artifacts (Signed URLs)
    public string? ViewerUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? DownloadUrl { get; set; }
}
