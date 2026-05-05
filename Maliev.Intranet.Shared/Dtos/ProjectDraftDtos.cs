using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Persisted draft state for the Project New page, stored in sessionStorage.
/// Enables seamless resume after JWT re-authentication without losing work.
/// </summary>
public sealed class DraftProjectState
{
    /// <summary>The temporary project identifier used for file uploads.</summary>
    public Guid TempProjectId { get; set; }

    /// <summary>The project title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The project description.</summary>
    public string? Description { get; set; }

    /// <summary>The selected customer's unique identifier.</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>The selected customer's display name.</summary>
    public string? CustomerName { get; set; }

    /// <summary>The selected customer's company name.</summary>
    public string? CustomerCompanyName { get; set; }

    /// <summary>The selected customer's email address.</summary>
    public string? CustomerEmail { get; set; }

    /// <summary>The selected customer's mobile phone number.</summary>
    public string? CustomerMobile { get; set; }

    /// <summary>The selected customer's landline phone number.</summary>
    public string? CustomerLandline { get; set; }

    /// <summary>The selected customer's company phone number.</summary>
    public string? CustomerCompanyPhone { get; set; }

    /// <summary>The selected lead time option code.</summary>
    public string SelectedLeadTimeCode { get; set; } = "STANDARD";

    /// <summary>The selected currency code (e.g. "THB", "USD"). Null restores the primary default.</summary>
    public string? SelectedCurrencyCode { get; set; }

    /// <summary>Shipping cost entered for draft quotation PDF generation.</summary>
    public decimal ShippingCost { get; set; }

    /// <summary>Manual discount entered for draft quotation PDF generation.</summary>
    public decimal ManualDiscountAmount { get; set; }

    /// <summary>Customer-facing quotation terms entered on the project page.</summary>
    public string? QuotationTerms { get; set; }

    /// <summary>Timestamp of the last modification to this draft.</summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// When non-null, the draft has been persisted to the ProjectService as a real Draft project.
    /// Used to resume the draft from the server after tab close or cross-device.
    /// </summary>
    public Guid? ServerProjectId { get; set; }

    /// <summary>The list of parts associated with this draft project.</summary>
    public List<DraftPartState> Parts { get; set; } = [];
}

/// <summary>
/// Persisted state for a single uploaded part within a draft project.
/// </summary>
public sealed class DraftPartState
{
    /// <summary>The unique file identifier assigned by the UploadService.</summary>
    public Guid FileId { get; set; }

    /// <summary>The server-assigned part ID returned by ProjectService after part creation. Null until the part is synced to the server.</summary>
    public Guid? ServerPartId { get; set; }

    /// <summary>The storage path of the uploaded file.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Previous source storage paths that may still receive late GeometryService events after
    /// the file has moved from temporary project storage into customer storage.
    /// </summary>
    public List<string> StoragePathAliases { get; set; } = [];

    /// <summary>The original file name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The ordered quantity for this part.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>The manufacturing process code (e.g. "FDM", "CNC").</summary>
    public string? ProcessCode { get; set; }

    /// <summary>The manufacturing process unique identifier.</summary>
    public Guid? ProcessId { get; set; }

    /// <summary>The material code.</summary>
    public string? MaterialCode { get; set; }

    /// <summary>The material unique identifier.</summary>
    public Guid? MaterialId { get; set; }

    /// <summary>The surface finish code.</summary>
    public string? SurfaceFinishCode { get; set; }

    /// <summary>The surface finish unique identifier.</summary>
    public Guid? SurfaceFinishId { get; set; }

    /// <summary>The tolerance code.</summary>
    public string? ToleranceCode { get; set; }

    /// <summary>The tolerance unique identifier.</summary>
    public Guid? ToleranceId { get; set; }

    /// <summary>Part-specific notes or instructions.</summary>
    public string? PartNotes { get; set; }

    /// <summary>True when the user has acknowledged all DFM warnings.</summary>
    public bool DfmAcknowledged { get; set; }

    // ── DFM reports (serialised as JSON so typed payloads survive a refresh) ──

    /// <summary>
    /// FDM DFM report serialised as a JSON string. Null until analysis completes or if
    /// the selected process is not FDM.
    /// </summary>
    public string? FdmDfmReportJson { get; set; }

    /// <summary>
    /// SLA/DLP DFM report serialised as a JSON string. Null until analysis completes or if
    /// the selected process is not SLA/DLP.
    /// </summary>
    public string? SlaDfmReportJson { get; set; }

    /// <summary>
    /// CNC DFM report serialised as a JSON string. Null until analysis completes or if
    /// the selected process is not CNC.
    /// </summary>
    public string? CncDfmReportJson { get; set; }

    // ── Pricing snapshot (shown instantly on restore; recalculated in background) ──

    /// <summary>Last computed unit price. Shown immediately on restore; recalculated in background.</summary>
    public decimal? EstimatedUnitPrice { get; set; }

    /// <summary>Last computed total amount (unit price × quantity). Shown immediately on restore.</summary>
    public decimal? EstimatedTotalAmount { get; set; }

    /// <summary>Last computed one-piece unit price before volume pricing. Shown immediately on restore.</summary>
    public decimal? EstimatedBaseUnitPrice { get; set; }

    /// <summary>Last computed discounted unit price before surface finish surcharge. Shown immediately on restore.</summary>
    public decimal? EstimatedDiscountedUnitPriceBeforeFinish { get; set; }

    /// <summary>Last computed unit price used to show surface finish option surcharges.</summary>
    public decimal? FinishPricingBaseUnitPrice { get; set; }

    /// <summary>Last computed per-unit surface finish surcharge.</summary>
    public decimal? FinishAdditionalUnitCost { get; set; }

    /// <summary>Additional process-specific configuration key-value pairs.</summary>
    public Dictionary<string, string> ProcessConfig { get; set; } = new();

    /// <summary>The part volume in cubic millimeters, extracted during file analysis.</summary>
    public double? VolumeMm3 { get; set; }

    /// <summary>The bounding box dimensions of the part.</summary>
    public FileAnalysisDimensionsDto? Dimensions { get; set; }

    /// <summary>Whether the part mesh is manifold (watertight). Null until analysis completes.</summary>
    public bool? IsManifold { get; set; }

    /// <summary>Human-readable explanation of why the mesh is non-manifold. Null when manifold or not yet determined.</summary>
    public string? NonManifoldReason { get; set; }

    /// <summary>Approximate count of broken/non-manifold faces. Null when manifold or not determined.</summary>
    public int? NonManifoldFaceCount { get; set; }

    /// <summary>Signed URL to the small (~256px) isometric thumbnail. Restored to avoid re-fetching on reload.</summary>
    public string? ThumbnailSmallUrl { get; set; }

    /// <summary>Signed URL to the large (~1200px) isometric thumbnail. Restored to avoid re-fetching on reload.</summary>
    public string? ThumbnailLargeUrl { get; set; }

    /// <summary>Raw GCS storage path for the small isometric thumbnail. Preserved to re-resolve signed URLs after expiry.</summary>
    public string? ThumbnailSmallGcsPath { get; set; }

    /// <summary>Raw GCS storage path for the large isometric thumbnail. Preserved to re-resolve signed URLs after expiry.</summary>
    public string? ThumbnailLargeGcsPath { get; set; }

    /// <summary>GCS storage path of the GLB artifact. Non-null when analysis produced a 3D model.</summary>
    public string? GlbStoragePath { get; set; }

    /// <summary>Signed URL for GLB viewer. Restores instantly on page reload.</summary>
    public string? ViewerUrl { get; set; }

    /// <summary>Pre-signed GLB URL delivered by GlbReady SignalR event.</summary>
    public string? GlbSignedUrl { get; set; }

    /// <summary>Technical drawing files attached to this part (PDF, DXF, DWG, images).</summary>
    public List<DraftProjectAttachmentDto> DrawingFiles { get; set; } = [];

    /// <summary>Supplementary files attached to this part (images, documents, archives).</summary>
    public List<DraftProjectAttachmentDto> SupplementaryFiles { get; set; } = [];

    // ── Manufacturing ────────────────────────────────────────────────────
    /// <summary>CNC surface roughness code (e.g. "Ra0.8").</summary>
    public string? RoughnessCode { get; set; }

    // ── Design & Features ─────────────────────────────────────────────────
    /// <summary>The type of part marking applied (engraving, laser, etc.).</summary>
    public PartMarkingType MarkingType { get; set; } = PartMarkingType.None;

    /// <summary>The text or content to be marked on the part, when applicable.</summary>
    public string? MarkingText { get; set; }

    /// <summary>Whether the part contains threaded holes.</summary>
    public bool HasThreadedHoles { get; set; }

    /// <summary>Thread specification string (e.g. "M3x0.5") when threaded holes are present.</summary>
    public string? ThreadedHoleSpec { get; set; }

    /// <summary>The number of threaded holes on the part. Meaningful only when <see cref="HasThreadedHoles"/> is <c>true</c>.</summary>
    public int ThreadedHoleCount { get; set; }

    /// <summary>Whether the part requires inserts.</summary>
    public bool HasInserts { get; set; }

    /// <summary>The type of insert to be installed (heat-set, press-fit, ultrasonic).</summary>
    public InsertType InsertType { get; set; } = InsertType.None;

    /// <summary>The number of inserts required on the part. Meaningful only when <see cref="InsertType"/> is not <see cref="InsertType.None"/>.</summary>
    public int InsertCount { get; set; }

    // ── Logistics ─────────────────────────────────────────────────────────
    /// <summary>Whether the part should be individually bagged and tagged for delivery.</summary>
    public bool BagAndTag { get; set; } = true;

    // ── Quality ───────────────────────────────────────────────────────────
    /// <summary>The required inspection level for this part.</summary>
    public InspectionLevel InspectionLevel { get; set; } = InspectionLevel.Standard;

    /// <summary>List of material or process certificates required (e.g. "EN10204-3.1", "RoHS").</summary>
    public List<string> Certificates { get; set; } = [];

    /// <summary>
    /// Raw GCS storage paths for overlay GLBs, keyed by "{PROCESS}__{category}".
    /// Persisted instead of signed URLs so they survive expiry. Re-signed on restore.
    /// </summary>
    public Dictionary<string, string>? OverlayPaths { get; set; }

    /// <summary>
    /// Number of distinct bodies in the CAD file. Null if not computed or single-body.
    /// </summary>
    public int? BodyCount { get; set; }

    /// <summary>
    /// Per-body metadata serialised as JSON. Null if not computed or single-body.
    /// </summary>
    public string? BodiesJson { get; set; }

    /// <summary>
    /// Zero-based index of the currently selected body. Null when no body is selected.
    /// </summary>
    public int? SelectedBodyIndex { get; set; }

    /// <summary>
    /// Per-part 3D viewer visual settings.
    /// </summary>
    public PartViewerSettings ViewerSettings { get; set; } = new();
}

/// <summary>
/// Persisted visual settings for the 3D part viewer.
/// </summary>
public sealed class PartViewerSettings
{
    /// <summary>The active render mode: solid, wireframe, or transparent.</summary>
    public string RenderMode { get; set; } = "solid";

    /// <summary>The active camera projection: perspective or orthographic.</summary>
    public string CameraProjection { get; set; } = "orthographic";

    /// <summary>True when edge rendering is enabled.</summary>
    public bool EdgesEnabled { get; set; }

    /// <summary>True when the grid floor is enabled.</summary>
    public bool GridEnabled { get; set; } = true;

    /// <summary>True when the part bounding box is enabled.</summary>
    public bool BoundingBoxEnabled { get; set; }

    /// <summary>True when section view is enabled.</summary>
    public bool SectionEnabled { get; set; }

    /// <summary>The section axis: x, y, or z.</summary>
    public string SectionAxis { get; set; } = "x";

    /// <summary>The section plane offset in millimeters.</summary>
    public double SectionOffsetMm { get; set; }

    /// <summary>True when the section clip plane is inverted.</summary>
    public bool SectionInverted { get; set; }

    /// <summary>Creates a detached copy.</summary>
    public PartViewerSettings Clone() => new()
    {
        RenderMode = RenderMode,
        CameraProjection = CameraProjection,
        EdgesEnabled = EdgesEnabled,
        GridEnabled = GridEnabled,
        BoundingBoxEnabled = BoundingBoxEnabled,
        SectionEnabled = SectionEnabled,
        SectionAxis = SectionAxis,
        SectionOffsetMm = SectionOffsetMm,
        SectionInverted = SectionInverted,
    };
}

/// <summary>
/// The kind of project attachment.
/// </summary>
public enum DraftAttachmentKind
{
    /// <summary>Technical drawing files (PDF, DXF, DWG, images).</summary>
    Drawing,
    /// <summary>Supplementary files (images, documents, archives).</summary>
    Supplementary
}

/// <summary>
/// A file attachment associated with a project part, stored in session draft state.
/// </summary>
public sealed class DraftProjectAttachmentDto
{
    /// <summary>The unique file identifier assigned by the UploadService.</summary>
    public Guid FileId { get; set; }

    /// <summary>GCS storage path of the file.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>The original file name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The MIME type of the file (e.g. "application/pdf", "image/png").</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>The file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>The kind of attachment (Drawing or Supplementary).</summary>
    public DraftAttachmentKind Kind { get; set; }

    /// <summary>UTC timestamp when the file was uploaded.</summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Describes the type of marking applied to a manufactured part.</summary>
public enum PartMarkingType
{
    /// <summary>No marking required.</summary>
    None,
    /// <summary>Mechanical engraving.</summary>
    Engraving,
    /// <summary>Laser marking or etching.</summary>
    Laser,
    /// <summary>Pad or ink printing.</summary>
    Printing,
    /// <summary>Self-adhesive sticker label.</summary>
    Sticker
}

/// <summary>Describes the type of threaded insert to be installed in a part.</summary>
public enum InsertType
{
    /// <summary>No insert required.</summary>
    None,
    /// <summary>Heat-set insert installed with a soldering iron or heat press.</summary>
    HeatSet,
    /// <summary>Press-fit insert installed under mechanical pressure.</summary>
    PressFit,
    /// <summary>Ultrasonic insert installed using ultrasonic vibration.</summary>
    Ultrasonic
}

/// <summary>Defines the quality inspection level required for a part.</summary>
public enum InspectionLevel
{
    /// <summary>Standard visual and basic dimensional check.</summary>
    Standard,
    /// <summary>Full dimensional inspection with measurement report.</summary>
    Dimensional,
    /// <summary>Complete CMM (coordinate measuring machine) inspection with full report.</summary>
    FullCmm
}
