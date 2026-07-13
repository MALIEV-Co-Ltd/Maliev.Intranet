using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Bff.Security;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time system notifications.
/// </summary>
/// <remarks>
/// A file identifier supplied by a browser is only a locator. UploadService re-authorizes the
/// current employee for the resource before the connection is admitted to its opaque group.
/// </remarks>
[RequirePermission(MalievPermissions.Project.Read, AuthenticationSchemes = "Bearer,Cookies")]
public class NotificationHub(UploadServiceClient uploadClient) : Hub
{
    /// <summary>
    /// Adds the current connection to an authorized file-specific SignalR group.
    /// </summary>
    /// <param name="fileId">The upload identifier for the file whose updates are requested.</param>
    [ResourceOwnership(ResourceOwnershipKind.DownstreamValidated, "UploadService", "fileId")]
    public async Task JoinFileGroup(Guid fileId)
    {
        var group = await AuthorizeFileGroupAsync(fileId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
    }

    /// <summary>
    /// Removes the current connection from an authorized file-specific SignalR group.
    /// </summary>
    /// <param name="fileId">The upload identifier for the file whose updates are no longer requested.</param>
    [ResourceOwnership(ResourceOwnershipKind.DownstreamValidated, "UploadService", "fileId")]
    public async Task LeaveFileGroup(Guid fileId)
    {
        var group = await AuthorizeFileGroupAsync(fileId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
    }

    /// <summary>Builds the opaque SignalR group used for an authorized uploaded file.</summary>
    public static string FileGroup(Guid fileId) => $"file:{fileId:N}";

    /// <summary>Builds an opaque file group only for a valid upload identifier.</summary>
    public static bool TryFileGroup(string? fileId, out string group)
    {
        if (Guid.TryParse(fileId, out var parsedFileId) && parsedFileId != Guid.Empty)
        {
            group = FileGroup(parsedFileId);
            return true;
        }

        group = string.Empty;
        return false;
    }

    private async Task<string> AuthorizeFileGroupAsync(Guid fileId)
    {
        if (fileId == Guid.Empty || !await uploadClient.CanReadFileAsync(fileId, Context.ConnectionAborted))
        {
            throw new HubException("File updates are unavailable.");
        }

        return FileGroup(fileId);
    }
}

/// <summary>
/// Payload pushed to the client when geometry analysis for an uploaded file completes.
/// </summary>
/// <param name="StoragePath">The GCS storage path of the original 3D file for client-side state reconciliation.</param>
/// <param name="UploadId">The upload ID of the file, if available.</param>
/// <param name="ThumbnailUrl">Signed URL to the generated thumbnail image, or null if unavailable.</param>
/// <param name="HiResThumbnailUrl">Signed URL to the 1000px ISO WebP thumbnail for hi-res display, or null if unavailable.</param>
/// <param name="Dimensions">Bounding box dimensions in millimetres, or null if analysis failed.</param>
/// <param name="PreviewUrls">Signed URLs for the six rendered preview images (front/back/left/right/top/bottom).</param>
/// <param name="Failed">True when geometry analysis failed (no preview or dimensions available).</param>
/// <param name="ErrorCode">Error code from the analysis failure, or null on success.</param>
/// <param name="BodyCount">Number of bodies in the CAD file (null if single-body or not computed).</param>
/// <param name="Bodies">Per-body metadata for multi-body files (null if single-body or not computed).</param>
/// <param name="NonManifoldReason">Human-readable explanation of why the mesh is non-manifold; null when manifold.</param>
/// <param name="NonManifoldFaceCount">Approximate count of broken/non-manifold faces; null when manifold.</param>
public record FileAnalysisCompletedPayload(
    string StoragePath,
    string? UploadId,
    string? ThumbnailUrl,
    string? HiResThumbnailUrl,
    FileAnalysisDimensions? Dimensions,
    FileAnalysisPreviewUrls? PreviewUrls,
    bool Failed,
    string? ErrorCode,
    int? BodyCount = null,
    IReadOnlyList<SignalRBodyInfo>? Bodies = null,
    string? NonManifoldReason = null,
    int? NonManifoldFaceCount = null);

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
/// <param name="BodyCount">Number of bodies in the CAD file (null if single-body or not computed).</param>
/// <param name="Bodies">Per-body metadata for multi-body files (null if single-body or not computed).</param>
/// <param name="ViewerStoragePath">GCS storage path loaded by the browser viewer.</param>
/// <param name="ViewerFileExtension">Dot-prefixed loader extension for the browser viewer.</param>
public record GlbReadyPayload(
    string StoragePath,
    string? GlbUrl,
    bool Failed,
    int? BodyCount = null,
    IReadOnlyList<SignalRBodyInfo>? Bodies = null,
    string? ViewerStoragePath = null,
    string? ViewerFileExtension = null);

/// <summary>Per-body metadata for multi-body CAD files, sent via SignalR.</summary>
/// <param name="Index">Zero-based body index.</param>
/// <param name="Name">Stable body name from glTF node or generated "Body_NN" format.</param>
/// <param name="VolumeCm3">Volume of this body in cm³ (null if not computed).</param>
/// <param name="BboxMin">Minimum XYZ coordinates in mm.</param>
/// <param name="BboxMax">Maximum XYZ coordinates in mm.</param>
public record SignalRBodyInfo(
    int Index,
    string Name,
    double? VolumeCm3,
    SignalRBBox BboxMin,
    SignalRBBox BboxMax);

/// <summary>3D bounding box with XYZ dimensions.</summary>
/// <param name="X">Width in millimetres.</param>
/// <param name="Y">Depth in millimetres.</param>
/// <param name="Z">Height in millimetres.</param>
public record SignalRBBox(double X, double Y, double Z);

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
/// <param name="NonManifoldReason">Human-readable mesh-integrity description; null when manifold.</param>
/// <param name="NonManifoldFaceCount">Approximate count of broken/non-manifold faces; null when manifold.</param>
public record DfmAnalysisReadyPayload(
    string StoragePath,
    Maliev.MessagingContracts.Contracts.Geometry.FdmDfmReportPayload? FdmReport,
    Maliev.MessagingContracts.Contracts.Geometry.SlaDfmReportPayload? SlaReport,
    Maliev.MessagingContracts.Contracts.Geometry.CncDfmReportPayload? CncReport,
    IReadOnlyDictionary<string, string>? OverlayUrls = null,
    IReadOnlyDictionary<string, string>? OverlayPaths = null,
    int? BodyCount = null,
    string? NonManifoldReason = null,
    int? NonManifoldFaceCount = null);

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
