namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Status of a file analysis operation, used for polling-based status checking.
/// </summary>
public sealed record FileAnalysisStatusDto
{
    public required string UploadId { get; init; }
    public required FileAnalysisStatus Status { get; init; }
    public FileAnalysisDimensionsDto? Dimensions { get; init; }
    public string? ThumbnailUrl { get; init; }
    public FileAnalysisPreviewUrlsDto? PreviewUrls { get; init; }
    public string? ErrorCode { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
    /// <summary>
    /// Whether the mesh is manifold (watertight). False indicates potential geometry issues.
    /// Null if not yet determined.
    /// </summary>
    public bool? IsManifold { get; init; }
    /// <summary>
    /// Independent preview image generation status. File appears "Ready" when Status=Completed
    /// regardless of this value.
    /// </summary>
    public PreviewProcessingStatus PreviewProcessingStatus { get; init; }
}

/// <summary>
/// Status values for file analysis.
/// </summary>
public enum FileAnalysisStatus
{
    Processing,
    Completed,
    Failed
}

/// <summary>
/// Independent preview image generation status, tracked separately from geometry analysis.
/// </summary>
public enum PreviewProcessingStatus
{
    /// <summary>Preview generation has not started yet.</summary>
    Pending,
    /// <summary>Preview images are being rendered.</summary>
    Processing,
    /// <summary>Preview images generated successfully.</summary>
    Completed,
    /// <summary>Preview generation failed.</summary>
    Failed
}

/// <summary>
/// Bounding box dimensions from geometry analysis.
/// </summary>
public sealed record FileAnalysisDimensionsDto
{
    /// <summary>Width of the bounding box in millimetres.</summary>
    public required double X { get; init; }
    /// <summary>Depth of the bounding box in millimetres.</summary>
    public required double Y { get; init; }
    /// <summary>Height of the bounding box in millimetres.</summary>
    public required double Z { get; init; }
    /// <summary>Part volume in cubic millimetres, converted from cm³ supplied by GeometryService.</summary>
    public double? VolumeMm3 { get; init; }
}

/// <summary>
/// Signed URLs for the six rendered preview images plus isometric view.
/// </summary>
public sealed record FileAnalysisPreviewUrlsDto
{
    public string? Front  { get; init; }
    public string? Back   { get; init; }
    public string? Left   { get; init; }
    public string? Right  { get; init; }
    public string? Top    { get; init; }
    public string? Bottom { get; init; }
    /// <summary>Isometric view preview image URL.</summary>
    public string? Iso    { get; init; }
}
