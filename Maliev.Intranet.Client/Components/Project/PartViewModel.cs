using System.Text.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Geometry;

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>
/// UI state wrapper for a single uploaded part. Combines upload state,
/// file analysis results, per-part configuration, and live pricing.
/// All config mutations flow through EventCallback to the parent page.
/// </summary>
public class PartViewModel
{
    // ── Persisted to DraftPartState ────────────────────────────────────

    /// <summary>The original file name / display name for this part.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The unique file identifier assigned by the UploadService.</summary>
    public Guid FileId { get; set; }

    /// <summary>The server-assigned part ID returned by ProjectService after part creation. Null until the part is synced to the server.</summary>
    public Guid? ServerPartId { get; set; }

    /// <summary>The storage path of the uploaded file.</summary>
    public string? StoragePath { get; set; }

    /// <summary>The manufacturing process code (e.g. "FDM", "CNC").</summary>
    public string? ProcessCode { get; set; }

    /// <summary>The manufacturing process unique identifier.</summary>
    public Guid? ProcessId { get; set; }

    /// <summary>The material code.</summary>
    public string? MaterialCode { get; set; }

    /// <summary>The material unique identifier.</summary>
    public Guid? MaterialId { get; set; }

    /// <summary>The surface finish code (maps to DraftPartState.SurfaceFinishCode).</summary>
    public string? FinishCode { get; set; }

    /// <summary>The surface finish unique identifier (maps to DraftPartState.SurfaceFinishId).</summary>
    public Guid? FinishId { get; set; }

    /// <summary>The tolerance code.</summary>
    public string? ToleranceCode { get; set; }

    /// <summary>The tolerance unique identifier.</summary>
    public Guid? ToleranceId { get; set; }

    /// <summary>The ordered quantity for this part.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Part-specific notes or instructions.</summary>
    public string? PartNotes { get; set; }

    /// <summary>True when the user has acknowledged all DFM warnings.</summary>
    public bool DfmAcknowledged { get; set; }

    // ── Upload / preview (from polling) ───────────────────────────────

    /// <summary>True while the file is being uploaded.</summary>
    public bool Uploading { get; set; }

    /// <summary>
    /// Upload progress percentage (0-100). Updated by JS Interop during HTTP upload.
    /// Resets to 0 when upload completes or fails.
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>True while waiting for the preview to be generated.</summary>
    public bool AwaitingPreview { get; set; }

    /// <summary>Small thumbnail (~256px). Source: status.PreviewUrls?.ThumbnailSmall ?? status.ThumbnailUrl</summary>
    public string? ThumbnailSmallUrl { get; set; }

    /// <summary>Large thumbnail (~1200px). Source: status.HiResThumbnailUrl ?? status.PreviewUrls?.ThumbnailLargeUrl</summary>
    public string? ThumbnailLargeUrl { get; set; }

    /// <summary>Raw GCS storage path for the small isometric thumbnail. Preserved to re-resolve signed URLs after expiry.</summary>
    public string? ThumbnailSmallGcsPath { get; set; }

    /// <summary>Raw GCS storage path for the large isometric thumbnail. Preserved to re-resolve signed URLs after expiry.</summary>
    public string? ThumbnailLargeGcsPath { get; set; }

    /// <summary>GLB artifact for BabylonJS viewer. Source: status.GlbStoragePath</summary>
    public string? GlbStoragePath { get; set; }

    /// <summary>Resolved signed viewer URL for BabylonJS inline rendering. Fetched on-demand when user opens 3D view.</summary>
    public string? ViewerUrl { get; set; }

    /// <summary>Pre-resolved signed URL for the GLB file, delivered by the GlbReady SignalR event. Bypasses API round-trip on cube click.</summary>
    public string? GlbSignedUrl { get; set; }

    /// <summary>True when the preview image failed to load.</summary>
    public bool PreviewLoadFailed { get; set; }

    /// <summary>Human-readable status string shown while the file is being processed.</summary>
    public string StatusText { get; set; } = "Processing...";

    /// <summary>The bounding box dimensions of the part, populated after file analysis.</summary>
    public FileAnalysisDimensionsDto? Dimensions { get; set; }

    /// <summary>The part volume in cubic millimeters, extracted during file analysis.</summary>
    public double? VolumeMm3 { get; set; }

    /// <summary>Whether the part mesh is manifold (watertight), populated after file analysis.</summary>
    public bool? IsManifold { get; set; }

    /// <summary>DFM analysis report embedded in FileAnalyzedEvent. Polymorphic — cast to FdmDfmReport, SlaDfmReport, or CncDfmReport as needed.</summary>
    public object? DfmReport { get; set; }

    /// <summary>FDM-specific DFM analysis report from <c>DfmAnalysisReadyEvent</c>.</summary>
    public object? FdmDfmReport { get; set; }

    /// <summary>SLA/DLP-specific DFM analysis report from <c>DfmAnalysisReadyEvent</c>.</summary>
    public object? SlaDfmReport { get; set; }

    /// <summary>CNC-specific DFM analysis report from <c>DfmAnalysisReadyEvent</c>.</summary>
    public object? CncDfmReport { get; set; }

    /// <summary>
    /// Signed overlay GLB URLs keyed by "{PROCESS}__{category}" (e.g. "FDM__thin_wall").
    /// Populated from the DfmAnalysisReady SignalR event. Used by PartDetailCard to wire
    /// clickable overlay toggling in the BabylonJS viewer.
    /// </summary>
    public Dictionary<string, string>? OverlayUrls { get; set; }

    /// <summary>
    /// Raw GCS storage paths for overlay GLBs (same keys as <see cref="OverlayUrls"/>).
    /// Persisted in draft state so signed URLs can be re-generated after expiry.
    /// </summary>
    public Dictionary<string, string>? OverlayPaths { get; set; }

    /// <summary>
    /// Resolves <see cref="DfmReport"/> from the per-process DFM report properties
    /// based on the currently selected <see cref="ProcessCode"/>.
    /// Call this after setting any of FdmDfmReport/SlaDfmReport/CncDfmReport,
    /// or after ProcessCode changes.
    /// </summary>
    public void ResolveDfmReport()
    {
        DfmReport = ProcessCode?.ToUpperInvariant() switch
        {
            "SLA" or "SLA_DLP" or "DLP" => SlaDfmReport,
            "CNC" or "CNC_MILL" or "CNC_TURN" => CncDfmReport,
            _ => FdmDfmReport,  // FDM, SLS, MJF, MJ, BJ, DMLS all use FDM report structure
        };
    }

    /// <summary>Error message if upload or analysis failed; null when healthy.</summary>
    public string? Error { get; set; }

    /// <summary>
    /// Set to true when DFM analysis fails (either by the client-side watchdog timer or
    /// when a <c>FileAnalysisCompleted</c> failure event arrives from the backend).
    /// Collapses the "Analyzing…" spinner into an "unavailable" notice in the overlay.
    /// </summary>
    public bool DfmAnalysisTimedOut { get; set; }

    /// <summary>
    /// Backend error code from a <c>FileAnalysisFailedEvent</c>, e.g. "GEOMETRY_PHASE2_TIMEOUT".
    /// Null when analysis completed normally or has not failed yet.
    /// </summary>
    public string? AnalysisErrorCode { get; set; }

    // ── Per-part attachments ───────────────────────────────────────────

    /// <summary>Technical drawing files attached to this part (PDF, DXF, DWG, images).</summary>
    public List<DraftProjectAttachmentDto> DrawingFiles { get; set; } = [];

    /// <summary>Supplementary files attached to this part (images, documents, archives).</summary>
    public List<DraftProjectAttachmentDto> SupplementaryFiles { get; set; } = [];

    // ── Pricing (UI-only, not persisted) ──────────────────────────────

    /// <summary>Estimated unit price from the last pricing calculation.</summary>
    public decimal? EstimatedUnitPrice { get; set; }

    /// <summary>Estimated total amount (unit price × quantity) from the last pricing calculation.</summary>
    public decimal? EstimatedTotalAmount { get; set; }

    /// <summary>Queue-aware lead time from last pricing result. Drives lead time panel day calculations.</summary>
    public int EstimatedLeadTimeDays { get; set; }

    /// <summary>True while a pricing request is in progress.</summary>
    public bool PricingLoading { get; set; }

    /// <summary>True when the last pricing request failed.</summary>
    public bool PricingFailed { get; set; }

    // ── Catalog (UI-only, not persisted) ──────────────────────────────

    /// <summary>Materials available for the selected process, populated on process selection.</summary>
    public List<CatalogMaterialDto> AvailableMaterials { get; set; } = [];

    /// <summary>Surface finishes available for the selected process + material, populated on material selection.</summary>
    public List<CatalogSurfaceFinishDto> AvailableFinishes { get; set; } = [];

    /// <summary>Tolerances available for the selected process, populated on process selection.</summary>
    public List<CatalogToleranceDto> AvailableTolerances { get; set; } = [];

    /// <summary>True while catalog data is being loaded.</summary>
    public bool CatalogLoading { get; set; }

    // ── New config fields (persisted to DraftPartState) ───────────────
    /// <summary>CNC surface roughness code (e.g. "Ra0.8"). Meaningful only when ProcessCode is "CNC".</summary>
    public string? RoughnessCode { get; set; }

    /// <summary>Part marking type. Defaults to <see cref="PartMarkingType.None"/>.</summary>
    public PartMarkingType MarkingType { get; set; } = PartMarkingType.None;

    /// <summary>Marking text content. Meaningful only when <see cref="MarkingType"/> is not <see cref="PartMarkingType.None"/>.</summary>
    public string? MarkingText { get; set; }

    /// <summary>True when the part requires threaded/tapped holes.</summary>
    public bool HasThreadedHoles { get; set; }

    /// <summary>Threaded hole specification (e.g. "M6"). Meaningful only when <see cref="HasThreadedHoles"/> is <c>true</c>.</summary>
    public string? ThreadedHoleSpec { get; set; }

    /// <summary>Number of threaded holes. Meaningful only when <see cref="HasThreadedHoles"/> is <c>true</c>.</summary>
    public int ThreadedHoleCount { get; set; }

    /// <summary>True when the part requires inserts (e.g. Helicoil).</summary>
    public bool HasInserts { get; set; }

    /// <summary>Insert type. Meaningful only when <see cref="HasInserts"/> is <c>true</c>.</summary>
    public InsertType InsertType { get; set; } = InsertType.None;

    /// <summary>Number of inserts. Meaningful only when <see cref="InsertType"/> is not <see cref="InsertType.None"/>.</summary>
    public int InsertCount { get; set; }

    /// <summary>True when the part should be individually bagged and tagged. Defaults to <c>true</c>.</summary>
    public bool BagAndTag { get; set; } = true;

    /// <summary>Inspection level for quality control. Defaults to <see cref="InspectionLevel.Standard"/>.</summary>
    public InspectionLevel InspectionLevel { get; set; } = InspectionLevel.Standard;

    /// <summary>Requested quality certificates (e.g. "ISO9001", "MaterialCert"). Disabled/display-only in UI for now.</summary>
    public List<string> Certificates { get; set; } = [];

    // ── Production routing (UI-only, not persisted) ───────────────────
    /// <summary>Production routing data fetched from the BFF. Null until loaded.</summary>
    public ProductionRoutingDto? ProductionRouting { get; set; }

    /// <summary>True while production routing data is being fetched.</summary>
    public bool ProductionRoutingLoading { get; set; }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>True when the part has a file, a process, and a material selected with no outstanding errors.</summary>
    public bool IsFullyConfigured =>
        ProcessId.HasValue && MaterialId.HasValue && FileId != Guid.Empty && Error == null;

    /// <summary>Maps this view model to a <see cref="DraftPartState"/> for session storage persistence.</summary>
    public DraftPartState ToDraftPartState() => new()
    {
        FileId = FileId,
        ServerPartId = ServerPartId,
        StoragePath = StoragePath ?? string.Empty,
        Name = Name,
        Quantity = Quantity,
        ProcessCode = ProcessCode,
        ProcessId = ProcessId,
        MaterialCode = MaterialCode,
        MaterialId = MaterialId,
        SurfaceFinishCode = FinishCode,
        SurfaceFinishId = FinishId,
        ToleranceCode = ToleranceCode,
        ToleranceId = ToleranceId,
        PartNotes = PartNotes,
        DfmAcknowledged = DfmAcknowledged,
        VolumeMm3 = VolumeMm3,
        Dimensions = Dimensions,
        IsManifold = IsManifold,
        ThumbnailSmallUrl = ThumbnailSmallUrl,
        ThumbnailLargeUrl = ThumbnailLargeUrl,
        ThumbnailSmallGcsPath = ThumbnailSmallGcsPath,
        ThumbnailLargeGcsPath = ThumbnailLargeGcsPath,
        GlbStoragePath = GlbStoragePath,
        DrawingFiles = DrawingFiles,
        SupplementaryFiles = SupplementaryFiles,
        RoughnessCode = RoughnessCode,
        MarkingType = MarkingType,
        MarkingText = MarkingText,
        HasThreadedHoles = HasThreadedHoles,
        ThreadedHoleSpec = ThreadedHoleSpec,
        ThreadedHoleCount = ThreadedHoleCount,
        HasInserts = HasInserts,
        InsertType = InsertType,
        InsertCount = InsertCount,
        BagAndTag = BagAndTag,
        InspectionLevel = InspectionLevel,
        Certificates = [..Certificates],
        // Persist pricing snapshot so the panel shows instantly on restore
        EstimatedUnitPrice = EstimatedUnitPrice,
        EstimatedTotalAmount = EstimatedTotalAmount,
        // Serialise typed DFM payloads to JSON so BuildDfmIssues works after restore
        FdmDfmReportJson = FdmDfmReport is FdmDfmReportPayload fdm ? JsonSerializer.Serialize(fdm) : null,
        SlaDfmReportJson = SlaDfmReport is SlaDfmReportPayload sla ? JsonSerializer.Serialize(sla) : null,
        CncDfmReportJson = CncDfmReport is CncDfmReportPayload cnc ? JsonSerializer.Serialize(cnc) : null,
        OverlayPaths = OverlayPaths,
    };

    /// <summary>Restores a <see cref="PartViewModel"/> from a persisted <see cref="DraftPartState"/>.</summary>
    public static PartViewModel FromDraftPartState(DraftPartState s)
    {
        var vm = new PartViewModel
        {
            FileId = s.FileId,
            ServerPartId = s.ServerPartId,
            StoragePath = s.StoragePath,
            Name = s.Name,
            Quantity = s.Quantity,
            ProcessCode = s.ProcessCode,
            ProcessId = s.ProcessId,
            MaterialCode = s.MaterialCode,
            MaterialId = s.MaterialId,
            FinishCode = s.SurfaceFinishCode,
            FinishId = s.SurfaceFinishId,
            ToleranceCode = s.ToleranceCode,
            ToleranceId = s.ToleranceId,
            PartNotes = s.PartNotes,
            DfmAcknowledged = s.DfmAcknowledged,
            VolumeMm3 = s.VolumeMm3,
            Dimensions = s.Dimensions,
            IsManifold = s.IsManifold,
            ThumbnailSmallUrl = s.ThumbnailSmallUrl,
            ThumbnailLargeUrl = s.ThumbnailLargeUrl,
            ThumbnailSmallGcsPath = s.ThumbnailSmallGcsPath,
            ThumbnailLargeGcsPath = s.ThumbnailLargeGcsPath,
            GlbStoragePath = s.GlbStoragePath,
            DrawingFiles = s.DrawingFiles,
            SupplementaryFiles = s.SupplementaryFiles,
            RoughnessCode = s.RoughnessCode,
            MarkingType = s.MarkingType,
            MarkingText = s.MarkingText,
            HasThreadedHoles = s.HasThreadedHoles,
            ThreadedHoleSpec = s.ThreadedHoleSpec,
            ThreadedHoleCount = s.ThreadedHoleCount,
            HasInserts = s.HasInserts,
            InsertType = s.InsertType,
            InsertCount = s.InsertCount,
            BagAndTag = s.BagAndTag,
            InspectionLevel = s.InspectionLevel,
            Certificates = [..s.Certificates],
            AwaitingPreview = false,
            StatusText = string.IsNullOrEmpty(s.ThumbnailSmallUrl) && string.IsNullOrEmpty(s.StoragePath)
                ? "Processing..."
                : "Ready",
            // Restore pricing snapshot for instant display while background recalculation runs
            EstimatedUnitPrice = s.EstimatedUnitPrice,
            EstimatedTotalAmount = s.EstimatedTotalAmount,
            OverlayPaths = s.OverlayPaths,
        };

        // Deserialise DFM reports from JSON so BuildDfmIssues pattern-matching works correctly.
        // Without this, the reports arrive as JsonElement after catch-up which never matches
        // FdmDfmReportPayload / SlaDfmReportPayload / CncDfmReportPayload.
        if (!string.IsNullOrEmpty(s.FdmDfmReportJson))
            vm.FdmDfmReport = JsonSerializer.Deserialize<FdmDfmReportPayload>(s.FdmDfmReportJson);
        if (!string.IsNullOrEmpty(s.SlaDfmReportJson))
            vm.SlaDfmReport = JsonSerializer.Deserialize<SlaDfmReportPayload>(s.SlaDfmReportJson);
        if (!string.IsNullOrEmpty(s.CncDfmReportJson))
            vm.CncDfmReport = JsonSerializer.Deserialize<CncDfmReportPayload>(s.CncDfmReportJson);

        // Resolve DfmReport from the typed per-process reports so overlay panels start correctly.
        vm.ResolveDfmReport();

        return vm;
    }
}
