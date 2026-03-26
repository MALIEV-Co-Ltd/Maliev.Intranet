namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// DTO for a 3D model file.
/// </summary>
public sealed record Model3DDto
{
    /// <summary>The unique identifier of the 3D model record.</summary>
    public Guid Id { get; set; }

    /// <summary>The internal file name of the 3D model as stored in the system.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>The original file name of the 3D model as provided by the user during upload.</summary>
    public string? OriginalFileName { get; set; }

    /// <summary>The size of the 3D model file in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>The MIME content type of the 3D model file (e.g., application/sla, model/stl).</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>The date and time when the 3D model was uploaded.</summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>The identifier of the user who uploaded the 3D model.</summary>
    public string? UploadedBy { get; set; }

    // Geometry Analysis
    /// <summary>Indicates whether the 3D model has undergone automated geometry analysis.</summary>
    public bool GeometryAnalyzed { get; set; }

    /// <summary>The calculated volume of the 3D model in cubic centimeters (cm³).</summary>
    public double? VolumeCm3 { get; set; }

    /// <summary>The estimated volume of support material required for printing, in cubic centimeters (cm³).</summary>
    public double? SupportVolumeCm3 { get; set; }

    /// <summary>The total surface area of the 3D model in square centimeters (cm²).</summary>
    public double? SurfaceAreaCm2 { get; set; }

    /// <summary>The width (X-dimension) of the model's bounding box in millimeters.</summary>
    public double? BoundingBoxX { get; set; }

    /// <summary>The depth (Y-dimension) of the model's bounding box in millimeters.</summary>
    public double? BoundingBoxY { get; set; }

    /// <summary>The height (Z-dimension) of the model's bounding box in millimeters.</summary>
    public double? BoundingBoxZ { get; set; }

    /// <summary>Indicates whether the 3D model is manifold (watertight) and suitable for printing.</summary>
    public bool? IsManifold { get; set; }

    /// <summary>The total number of triangles in the 3D model's mesh.</summary>
    public int? TriangleCount { get; set; }

    // Viewer Artifacts (Signed URLs)
    /// <summary>A temporary signed URL for viewing the 3D model in the browser.</summary>
    public string? ViewerUrl { get; set; }

    /// <summary>A temporary signed URL for the 3D model's thumbnail image.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>A temporary signed URL for downloading the original 3D model file.</summary>
    public string? DownloadUrl { get; set; }
}
