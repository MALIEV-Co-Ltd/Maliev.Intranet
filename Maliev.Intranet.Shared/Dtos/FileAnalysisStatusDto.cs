namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Status of a file analysis operation, used for polling-based status checking.
/// </summary>
public sealed record FileAnalysisStatusDto
{
    /// <summary>Gets or sets the unique identifier of the file upload.</summary>
    public required string UploadId { get; init; }

    /// <summary>Gets or sets the current analysis status of the file.</summary>
    public required FileAnalysisStatus Status { get; init; }

    /// <summary>Gets or sets the bounding box dimensions and volume if analysis is complete.</summary>
    public FileAnalysisDimensionsDto? Dimensions { get; init; }

    /// <summary>Gets or sets the URL for a generated thumbnail image of the model.</summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>Signed URL to the 1000px ISO WebP thumbnail. Preferred over ThumbnailUrl for detail card display.</summary>
    public string? HiResThumbnailUrl { get; init; }

    /// <summary>Gets or sets the collection of preview image URLs from different perspectives.</summary>
    public FileAnalysisPreviewUrlsDto? PreviewUrls { get; init; }

    /// <summary>
    /// GCS storage path of the GLB artifact produced by GeometryService.
    /// Null until the file analysis event is consumed.
    /// </summary>
    public string? GlbStoragePath { get; init; }

    /// <summary>
    /// GCS storage path the browser viewer should load.
    /// For browser-loadable mesh uploads, this may be the original storage path.
    /// </summary>
    public string? ViewerStoragePath { get; init; }

    /// <summary>
    /// Dot-prefixed extension for the three.js viewer to load, such as .glb, .stl, or .obj.
    /// </summary>
    public string? ViewerFileExtension { get; init; }

    /// <summary>
    /// Pre-signed URL for the GLB viewer artifact. Null until FileAnalyzed event is consumed.
    /// Client should prefer this over calling the viewer-url API.
    /// </summary>
    public string? GlbSignedUrl { get; init; }

    /// <summary>Gets or sets the error code if the analysis failed.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Gets or sets the UTC timestamp when the analysis was processed.</summary>
    public DateTimeOffset? ProcessedAt { get; init; }

    /// <summary>
    /// Whether the mesh is manifold (watertight). False indicates potential geometry issues.
    /// Null if not yet determined.
    /// </summary>
    public bool? IsManifold { get; init; }

    /// <summary>
    /// Human-readable explanation of why the mesh is non-manifold (e.g. "5 open boundary edges").
    /// Null when IsManifold is true or not yet determined.
    /// </summary>
    public string? NonManifoldReason { get; init; }

    /// <summary>
    /// Approximate number of broken/non-manifold faces. Null when IsManifold is true or not determined.
    /// </summary>
    public int? NonManifoldFaceCount { get; init; }

    /// <summary>
    /// Independent preview image generation status. File appears "Ready" when Status=Completed
    /// regardless of this value.
    /// </summary>
    public PreviewProcessingStatus PreviewProcessingStatus { get; init; }

    /// <summary>
    /// DFM analysis report (FdmDfmReport, SlaDfmReport, or CncDfmReport) embedded in FileAnalyzedEvent.
    /// Null until the DFM analysis event is consumed.
    /// </summary>
    public object? DfmReport { get; init; }
}

/// <summary>
/// Status values for file analysis.
/// </summary>
public enum FileAnalysisStatus
{
    /// <summary>The file is currently being analyzed.</summary>
    Processing,
    /// <summary>The file analysis has completed successfully.</summary>
    Completed,
    /// <summary>The file analysis has failed.</summary>
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
/// Signed URLs for the six rendered preview images plus isometric views.
/// </summary>
public sealed record FileAnalysisPreviewUrlsDto
{
    /// <summary>Gets or sets the front view preview image URL (small WebP).</summary>
    public string? FrontSmall { get; init; }
    /// <summary>Gets or sets the back view preview image URL (small WebP).</summary>
    public string? BackSmall { get; init; }
    /// <summary>Gets or sets the left view preview image URL (small WebP).</summary>
    public string? LeftSmall { get; init; }
    /// <summary>Gets or sets the right view preview image URL (small WebP).</summary>
    public string? RightSmall { get; init; }
    /// <summary>Gets or sets the top view preview image URL (small WebP).</summary>
    public string? TopSmall { get; init; }
    /// <summary>Gets or sets the bottom view preview image URL (small WebP).</summary>
    public string? BottomSmall { get; init; }
    /// <summary>Isometric thumbnail WebP URL (~256px small).</summary>
    public string? ThumbnailSmall { get; init; }
    /// <summary>Isometric thumbnail WebP URL (1200px large, hi-res detail card display).</summary>
    public string? ThumbnailLargeUrl { get; init; }
    /// <summary>Raw GCS storage path for the small isometric thumbnail. Preserved to allow re-signing after URL expiry.</summary>
    public string? ThumbnailSmallGcsPath { get; init; }
    /// <summary>Raw GCS storage path for the large isometric thumbnail. Preserved to allow re-signing after URL expiry.</summary>
    public string? ThumbnailLargeGcsPath { get; init; }
}
