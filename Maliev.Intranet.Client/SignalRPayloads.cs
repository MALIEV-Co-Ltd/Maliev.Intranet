using Maliev.MessagingContracts.Contracts.Geometry;

namespace Maliev.Intranet.Client;

/// <summary>
/// Client-side mirror of the BFF's FileAnalysisCompletedPayload, used for SignalR deserialization.
/// Covers both SmallThumbnailReadyConsumer (ThumbnailUrl only) and PreviewImagesGeneratedConsumer (full PreviewUrls).
/// </summary>
public sealed class SignalRFileAnalysisPayload
{
    /// <summary>GCS storage path of the source file.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Small thumbnail URL, set by SmallThumbnailReadyConsumer.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Hi-res thumbnail URL, set by PreviewImagesGeneratedConsumer.</summary>
    public string? HiResThumbnailUrl { get; set; }

    /// <summary>Bounding box dimensions in millimetres, or null if analysis failed.</summary>
    public SignalRDimensions? Dimensions { get; set; }

    /// <summary>Full set of preview URLs, set by PreviewImagesGeneratedConsumer when processing is complete.</summary>
    public SignalRPreviewUrls? PreviewUrls { get; set; }

    /// <summary>True when preview generation failed.</summary>
    public bool Failed { get; set; }

    /// <summary>Error code when Failed is true.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Number of bodies in the CAD file (null if single-body or not computed).</summary>
    public int? BodyCount { get; set; }

    /// <summary>Per-body metadata for multi-body files (null if single-body or not computed).</summary>
    public List<SignalRBodyInfo>? Bodies { get; set; }

    /// <summary>Human-readable explanation of why the mesh is non-manifold (null when manifold).</summary>
    public string? NonManifoldReason { get; set; }

    /// <summary>Approximate count of broken/non-manifold faces (null when manifold).</summary>
    public int? NonManifoldFaceCount { get; set; }
}

/// <summary>
/// Client-side mirror of FileAnalysisDimensions from NotificationHub.
/// </summary>
public sealed class SignalRDimensions
{
    /// <summary>Width in millimetres.</summary>
    public double X { get; set; }

    /// <summary>Depth in millimetres.</summary>
    public double Y { get; set; }

    /// <summary>Height in millimetres.</summary>
    public double Z { get; set; }

    /// <summary>Part volume in cubic millimetres.</summary>
    public double? VolumeMm3 { get; set; }
}

/// <summary>
/// Client-side mirror of FileAnalysisPreviewUrls from NotificationHub.
/// </summary>
public sealed class SignalRPreviewUrls
{
    /// <summary>Small (~256px) isometric thumbnail URL.</summary>
    public string? ThumbnailSmall { get; set; }

    /// <summary>Large (~1200px) isometric thumbnail URL.</summary>
    public string? ThumbnailLarge { get; set; }

    /// <summary>Front-face small preview URL.</summary>
    public string? FrontSmall { get; set; }

    /// <summary>Back-face small preview URL.</summary>
    public string? BackSmall { get; set; }

    /// <summary>Left-face small preview URL.</summary>
    public string? LeftSmall { get; set; }

    /// <summary>Right-face small preview URL.</summary>
    public string? RightSmall { get; set; }

    /// <summary>Top-face small preview URL.</summary>
    public string? TopSmall { get; set; }

    /// <summary>Bottom-face small preview URL.</summary>
    public string? BottomSmall { get; set; }

    /// <summary>Raw GCS storage path for the small isometric thumbnail.</summary>
    public string? ThumbnailSmallGcsPath { get; set; }

    /// <summary>Raw GCS storage path for the large isometric thumbnail.</summary>
    public string? ThumbnailLargeGcsPath { get; set; }
}

/// <summary>
/// Client-side mirror of GlbReadyPayload from NotificationHub.
/// </summary>
public sealed class SignalRGlbReadyPayload
{
    /// <summary>GCS storage path of the source file.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Pre-resolved signed URL for the GLB artifact.</summary>
    public string? GlbUrl { get; set; }

    /// <summary>True when GLB generation failed.</summary>
    public bool Failed { get; set; }

    /// <summary>Number of bodies in the CAD file (null if single-body or not computed).</summary>
    public int? BodyCount { get; set; }

    /// <summary>Per-body metadata for multi-body files (null if single-body or not computed).</summary>
    public List<SignalRBodyInfo>? Bodies { get; set; }
}

/// <summary>
/// Client-side mirror of DfmAnalysisReadyPayload from NotificationHub.
/// Carries process-specific DFM analysis results for FDM, SLA, and CNC.
/// </summary>
public sealed class SignalRDfmAnalysisPayload
{
    /// <summary>GCS storage path of the original file (join key).</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>FDM-specific DFM analysis data, or null if not applicable.</summary>
    public FdmDfmReportPayload? FdmReport { get; set; }

    /// <summary>SLA/DLP-specific DFM analysis data, or null if not applicable.</summary>
    public SlaDfmReportPayload? SlaReport { get; set; }

    /// <summary>CNC-specific DFM analysis data, or null if not applicable.</summary>
    public CncDfmReportPayload? CncReport { get; set; }

    /// <summary>
    /// Signed GCS download URLs for per-issue overlay GLBs, keyed by "{PROCESS}__{category}"
    /// (e.g. "FDM__thin_wall"). Load these on-demand when the user clicks a DFM issue.
    /// Null if no overlays were generated.
    /// </summary>
    public Dictionary<string, string>? OverlayUrls { get; set; }

    /// <summary>
    /// Raw GCS storage paths for overlay GLBs (same keys as <see cref="OverlayUrls"/>).
    /// Persisted in draft state so signed URLs can be re-generated after expiry.
    /// </summary>
    public Dictionary<string, string>? OverlayPaths { get; set; }

    /// <summary>
    /// Number of distinct bodies/shells detected in the mesh. Greater than 1 means multi-body.
    /// </summary>
    public int? BodyCount { get; set; }

    /// <summary>Human-readable mesh-integrity description forwarded from tessellation; null when manifold.</summary>
    public string? NonManifoldReason { get; set; }

    /// <summary>Approximate count of broken/non-manifold faces; null when manifold.</summary>
    public int? NonManifoldFaceCount { get; set; }
}

/// <summary>
/// Client-side mirror of SignalRBodyInfo from NotificationHub.
/// Per-body metadata for multi-body CAD files.
/// </summary>
public sealed class SignalRBodyInfo
{
    /// <summary>Zero-based body index.</summary>
    public int Index { get; set; }

    /// <summary>Stable body name from glTF node or generated "Body_NN" format.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Volume of this body in cm³ (null if not computed).</summary>
    public double? VolumeCm3 { get; set; }

    /// <summary>Minimum XYZ coordinates in mm.</summary>
    public SignalRBBox BboxMin { get; set; } = new();

    /// <summary>Maximum XYZ coordinates in mm.</summary>
    public SignalRBBox BboxMax { get; set; } = new();
}

/// <summary>
/// 3D bounding box with XYZ dimensions.
/// </summary>
public sealed class SignalRBBox
{
    /// <summary>Width in millimetres.</summary>
    public double X { get; set; }

    /// <summary>Depth in millimetres.</summary>
    public double Y { get; set; }

    /// <summary>Height in millimetres.</summary>
    public double Z { get; set; }
}
