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

    /// <summary>Full set of preview URLs, set by PreviewImagesGeneratedConsumer when processing is complete.</summary>
    public SignalRPreviewUrls? PreviewUrls { get; set; }

    /// <summary>True when preview generation failed.</summary>
    public bool Failed { get; set; }

    /// <summary>Error code when Failed is true.</summary>
    public string? ErrorCode { get; set; }
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
}
