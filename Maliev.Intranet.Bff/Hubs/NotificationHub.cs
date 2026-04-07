using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time system notifications.
/// </summary>
public class NotificationHub : Hub
{
    /// <summary>
    /// Broadcasts a notification message to all connected clients.
    /// </summary>
    /// <param name="message">The notification message content.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task SendNotification(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }

    /// <summary>
    /// Notifies all connected clients that customer data has changed.
    /// </summary>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task NotifyCustomerChanged()
    {
        await Clients.All.SendAsync("CustomerChanged");
    }

    /// <summary>
    /// Adds the current connection to the file-specific SignalR group.
    /// Call this after uploading a 3D file to receive analysis events for that file only.
    /// </summary>
    /// <param name="storagePath">GCS storage path of the uploaded file (used as group key).</param>
    public async Task JoinFileGroup(string storagePath)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"file:{storagePath}");

    /// <summary>
    /// Removes the current connection from the file-specific SignalR group.
    /// Call this when the file is removed or the page is left.
    /// </summary>
    /// <param name="storagePath">GCS storage path of the file to leave.</param>
    public async Task LeaveFileGroup(string storagePath)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"file:{storagePath}");
}

/// <summary>
/// Payload pushed to the client when geometry analysis for an uploaded file completes.
/// </summary>
/// <param name="StoragePath">The GCS storage path of the original 3D file (used as join key on the client).</param>
/// <param name="UploadId">The upload ID of the file, if available.</param>
/// <param name="ThumbnailUrl">Signed URL to the generated thumbnail image, or null if unavailable.</param>
/// <param name="HiResThumbnailUrl">Signed URL to the 1000px ISO WebP thumbnail for hi-res display, or null if unavailable.</param>
/// <param name="Dimensions">Bounding box dimensions in millimetres, or null if analysis failed.</param>
/// <param name="PreviewUrls">Signed URLs for the six rendered preview images (front/back/left/right/top/bottom).</param>
/// <param name="Failed">True when geometry analysis failed (no preview or dimensions available).</param>
/// <param name="ErrorCode">Error code from the analysis failure, or null on success.</param>
public record FileAnalysisCompletedPayload(
    string StoragePath,
    string? UploadId,
    string? ThumbnailUrl,
    string? HiResThumbnailUrl,
    FileAnalysisDimensions? Dimensions,
    FileAnalysisPreviewUrls? PreviewUrls,
    bool Failed,
    string? ErrorCode);

/// <summary>
/// Bounding-box dimensions from geometry analysis.
/// </summary>
/// <param name="X">Width in millimetres.</param>
/// <param name="Y">Depth in millimetres.</param>
/// <param name="Z">Height in millimetres.</param>
/// <param name="VolumeMm3">Part volume in cubic millimetres.</param>
public record FileAnalysisDimensions(double X, double Y, double Z, double? VolumeMm3);

/// <summary>
/// Signed URLs for the six rendered preview images plus isometric views.
/// </summary>
/// <param name="FrontSmall">Front view URL (small WebP).</param>
/// <param name="BackSmall">Back view URL (small WebP).</param>
/// <param name="LeftSmall">Left view URL (small WebP).</param>
/// <param name="RightSmall">Right view URL (small WebP).</param>
/// <param name="TopSmall">Top view URL (small WebP).</param>
/// <param name="BottomSmall">Bottom view URL (small WebP).</param>
/// <param name="ThumbnailSmall">Isometric thumbnail URL (small ~256px WebP).</param>
/// <param name="ThumbnailLarge">Isometric thumbnail URL (large 1200px WebP, hi-res display).</param>
/// <param name="ThumbnailSmallGcsPath">Raw GCS storage path for the small isometric thumbnail.</param>
/// <param name="ThumbnailLargeGcsPath">Raw GCS storage path for the large isometric thumbnail.</param>
public record FileAnalysisPreviewUrls(
    string? FrontSmall,
    string? BackSmall,
    string? LeftSmall,
    string? RightSmall,
    string? TopSmall,
    string? BottomSmall,
    string? ThumbnailSmall,
    string? ThumbnailLarge = null,
    string? ThumbnailSmallGcsPath = null,
    string? ThumbnailLargeGcsPath = null);

/// <summary>Payload pushed when the GLB 3D model file is ready for viewing.</summary>
/// <param name="StoragePath">GCS path of the original file (join key).</param>
/// <param name="GlbUrl">Signed URL to the GLB file, or null if generation failed.</param>
/// <param name="Failed">True when GLB generation failed.</param>
public record GlbReadyPayload(string StoragePath, string? GlbUrl, bool Failed);

/// <summary>Payload pushed when DFM analysis results are ready for all process types.</summary>
/// <param name="StoragePath">GCS path of the original file (join key).</param>
/// <param name="FdmReport">FDM-specific DFM analysis data, or null if not applicable.</param>
/// <param name="SlaReport">SLA-specific DFM analysis data, or null if not applicable.</param>
/// <param name="CncReport">CNC-specific DFM analysis data, or null if not applicable.</param>
/// <param name="OverlayUrls">
/// Signed GCS download URLs for per-issue overlay GLBs, keyed by "{PROCESS}__{category}".
/// Frontend loads these on-demand when a DFM issue is clicked. Null if none were generated.
/// </param>
/// <param name="OverlayPaths">
/// Raw GCS storage paths for overlay GLBs (same keys as <paramref name="OverlayUrls"/>).
/// Persisted in draft state so signed URLs can be re-generated after expiry.
/// </param>
/// <param name="BodyCount">Number of distinct bodies/shells detected; greater than 1 means multi-body.</param>
public record DfmAnalysisReadyPayload(
    string StoragePath,
    Maliev.MessagingContracts.Contracts.Geometry.FdmDfmReportPayload? FdmReport,
    Maliev.MessagingContracts.Contracts.Geometry.SlaDfmReportPayload? SlaReport,
    Maliev.MessagingContracts.Contracts.Geometry.CncDfmReportPayload? CncReport,
    IReadOnlyDictionary<string, string>? OverlayUrls = null,
    IReadOnlyDictionary<string, string>? OverlayPaths = null,
    int? BodyCount = null);

/// <summary>Payload pushed when a pricing calculation result is ready.</summary>
/// <param name="StoragePath">GCS path of the original file (join key).</param>
/// <param name="UnitPrice">Calculated unit price.</param>
/// <param name="TotalPrice">Total price for the requested quantity.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="EstimatedLeadTimeDays">Estimated days for production and shipping, or null if unknown.</param>
/// <param name="ValidUntil">When this price quote expires.</param>
public record PriceCalculatedPayload(
    string StoragePath,
    double UnitPrice,
    double TotalPrice,
    string Currency,
    int? EstimatedLeadTimeDays,
    DateTimeOffset ValidUntil);
