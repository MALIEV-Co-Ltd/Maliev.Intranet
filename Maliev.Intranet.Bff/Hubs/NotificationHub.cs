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
}

/// <summary>
/// Payload pushed to the client when geometry analysis for an uploaded file completes.
/// </summary>
/// <param name="StoragePath">The GCS storage path of the original 3D file (used as join key on the client).</param>
/// <param name="UploadId">The upload ID of the file, if available.</param>
/// <param name="ThumbnailUrl">Signed URL to the generated thumbnail image, or null if unavailable.</param>
/// <param name="Dimensions">Bounding box dimensions in millimetres, or null if analysis failed.</param>
/// <param name="PreviewUrls">Signed URLs for the six rendered preview images (front/back/left/right/top/bottom).</param>
/// <param name="Failed">True when geometry analysis failed (no preview or dimensions available).</param>
/// <param name="ErrorCode">Error code from the analysis failure, or null on success.</param>
public record FileAnalysisCompletedPayload(
    string StoragePath,
    string? UploadId,
    string? ThumbnailUrl,
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
/// Signed URLs for the six rendered preview images plus isometric view.
/// </summary>
/// <param name="Front">Front view URL.</param>
/// <param name="Back">Back view URL.</param>
/// <param name="Left">Left view URL.</param>
/// <param name="Right">Right view URL.</param>
/// <param name="Top">Top view URL.</param>
/// <param name="Bottom">Bottom view URL.</param>
/// <param name="Iso">Isometric view URL.</param>
public record FileAnalysisPreviewUrls(
    string? Front,
    string? Back,
    string? Left,
    string? Right,
    string? Top,
    string? Bottom,
    string? Iso);
