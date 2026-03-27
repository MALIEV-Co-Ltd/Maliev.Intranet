using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

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

    /// <summary>Error message if upload or analysis failed; null when healthy.</summary>
    public string? Error { get; set; }

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

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>True when the part has a file, a process, and a material selected with no outstanding errors.</summary>
    public bool IsFullyConfigured =>
        ProcessId.HasValue && MaterialId.HasValue && FileId != Guid.Empty && Error == null;

    /// <summary>Maps this view model to a <see cref="DraftPartState"/> for session storage persistence.</summary>
    public DraftPartState ToDraftPartState() => new()
    {
        FileId = FileId,
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
        GlbStoragePath = GlbStoragePath,
    };

    /// <summary>Restores a <see cref="PartViewModel"/> from a persisted <see cref="DraftPartState"/>.</summary>
    public static PartViewModel FromDraftPartState(DraftPartState s) => new()
    {
        FileId = s.FileId,
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
        GlbStoragePath = s.GlbStoragePath,
        AwaitingPreview = false,
        StatusText = string.IsNullOrEmpty(s.ThumbnailSmallUrl) && string.IsNullOrEmpty(s.StoragePath)
            ? "Processing..."
            : "Ready",
    };
}
