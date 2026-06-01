using System.ComponentModel.DataAnnotations;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Summary DTO for a project in list views.
/// </summary>
public class ProjectSummaryDto
{
    /// <summary>Gets or sets the unique project identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable project number (e.g. PRJ-2025-0042).</summary>
    public string ProjectNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the associated customer.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the customer ID.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the project title as provided by the customer.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the current lifecycle status of the project.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the total number of parts in this project.</summary>
    public int PartsCount { get; set; }

    /// <summary>Gets or sets the total confirmed price across all parts.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>Gets or sets the current quotation version number, if a quotation has been generated.</summary>
    public int? CurrentQuotationVersionNumber { get; set; }

    /// <summary>Gets or sets small preview records for the first parts in this project.</summary>
    public List<ProjectPartPreviewDto> PartPreviews { get; set; } = [];

    /// <summary>Gets or sets the date the project was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Compact part preview for project list views.
/// </summary>
public class ProjectPartPreviewDto
{
    /// <summary>Gets or sets the unique part identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the sequential part number within the project.</summary>
    public int PartNumber { get; set; }

    /// <summary>Gets or sets the original uploaded filename.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw uploaded file reference.</summary>
    public string? FileReference { get; set; }

    /// <summary>Gets or sets the signed thumbnail URL for display.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Gets or sets the raw GCS path for the small thumbnail artifact.</summary>
    public string? ThumbnailSmallGcsPath { get; set; }

    /// <summary>Gets or sets the raw GCS path for the large thumbnail artifact.</summary>
    public string? ThumbnailLargeGcsPath { get; set; }

    /// <summary>Gets or sets the manufacturing process type.</summary>
    public string? ProcessType { get; set; }

    /// <summary>Gets or sets the material display name.</summary>
    public string? MaterialName { get; set; }

    /// <summary>Gets or sets the ordered quantity.</summary>
    public int Quantity { get; set; }
}

/// <summary>
/// Full detail DTO for a single project including its parts.
/// </summary>
public class ProjectDetailDto
{
    /// <summary>Gets or sets the unique project identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable project number.</summary>
    public string ProjectNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the customer ID.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the customer's profile image URL.</summary>
    public string? CustomerProfileImageUrl { get; set; }

    /// <summary>Gets or sets the customer email address.</summary>
    public string? CustomerEmail { get; set; }

    /// <summary>Gets or sets the customer phone number.</summary>
    public string? CustomerPhone { get; set; }

    /// <summary>Gets or sets the customer lifecycle status.</summary>
    public string? CustomerStatus { get; set; }

    /// <summary>Gets or sets the customer market segment.</summary>
    public string? CustomerSegment { get; set; }

    /// <summary>Gets or sets the customer business tier.</summary>
    public string? CustomerTier { get; set; }

    /// <summary>Gets or sets the customer preferred language.</summary>
    public string? CustomerPreferredLanguage { get; set; }

    /// <summary>Gets or sets the customer's local timezone.</summary>
    public string? CustomerTimezone { get; set; }

    /// <summary>Gets or sets the associated company ID.</summary>
    public Guid? CustomerCompanyId { get; set; }

    /// <summary>Gets or sets the associated company name.</summary>
    public string? CustomerCompanyName { get; set; }

    /// <summary>Gets or sets the company's contact phone number.</summary>
    public string? CustomerCompanyPhone { get; set; }

    /// <summary>Gets or sets the company's contact email address.</summary>
    public string? CustomerCompanyEmail { get; set; }

    /// <summary>Gets or sets the customer's company tax identification number.</summary>
    public string? CustomerTaxId { get; set; }

    /// <summary>Gets or sets the customer's billing branch label.</summary>
    public string? CustomerBranch { get; set; }

    /// <summary>Gets or sets the formatted billing address.</summary>
    public string? BillingAddressLine { get; set; }

    /// <summary>Gets or sets the formatted shipping address.</summary>
    public string? ShippingAddressLine { get; set; }

    /// <summary>Gets or sets the shipping recipient name.</summary>
    public string? ShippingRecipientName { get; set; }

    /// <summary>Gets or sets the shipping recipient phone number.</summary>
    public string? ShippingRecipientPhone { get; set; }

    /// <summary>Gets or sets the project title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description of the project scope.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the current lifecycle status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets internal notes about this project.</summary>
    public List<ProjectNoteDto>? Notes { get; set; }

    /// <summary>Gets or sets the date until which the quotation is valid.</summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>Gets or sets the total confirmed price across all parts.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>Gets or sets the currency code (e.g. THB).</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Gets or sets the associated quotation ID once generated.</summary>
    public Guid? QuotationId { get; set; }

    /// <summary>Gets or sets the human-readable quotation number once assigned.</summary>
    public string? QuotationNumber { get; set; }

    /// <summary>Gets or sets the quotation status once the quotation has been generated.</summary>
    public string? QuotationStatus { get; set; }

    /// <summary>Gets or sets the current quotation version ID once generated.</summary>
    public Guid? CurrentQuotationVersionId { get; set; }

    /// <summary>Gets or sets the current quotation version number once generated.</summary>
    public int? CurrentQuotationVersionNumber { get; set; }

    /// <summary>Gets or sets the source project identifier when this project is a duplicate/reorder draft.</summary>
    public Guid? SourceProjectId { get; set; }

    /// <summary>Gets or sets the source project number when this project is a duplicate/reorder draft.</summary>
    public string? SourceProjectNumber { get; set; }

    /// <summary>Gets or sets the user ID who created this project.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Gets or sets the display name of the user who created this project.</summary>
    public string? CreatedByName { get; set; }

    /// <summary>Gets or sets the date the project was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the date the project was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the list of parts belonging to this project.</summary>
    public List<ProjectPartDto> Parts { get; set; } = new();

    /// <summary>Gets or sets the status timeline events for this project.</summary>
    public List<ProjectTimelineEventDto> Timeline { get; set; } = new();
}

/// <summary>
/// DTO for a project internal note.
/// </summary>
public class ProjectNoteDto
{
    /// <summary>Gets or sets the note unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the project ID.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the author display name.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Gets or sets the author principal ID.</summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>Gets or sets the note content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp of creation.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to add an internal note to a project.
/// </summary>
public class AddProjectNoteRequest
{
    /// <summary>Gets or sets the note content.</summary>
    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Represents a single manufacturable part within a project.
/// </summary>
public class ProjectPartDto
{
    /// <summary>Gets or sets the unique part identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the reference to the uploaded 3D file.</summary>
    public Guid FileId { get; set; }

    /// <summary>Gets or sets the GCS storage path for the uploaded file.</summary>
    public string? FileReference { get; set; }

    /// <summary>Gets or sets the original uploaded filename (e.g. bracket_v2.stl).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the manufacturing process type (FDM, SLA, CNC, etc.).</summary>
    public string? ProcessType { get; set; }

    /// <summary>Gets or sets the material ID.</summary>
    public Guid? MaterialId { get; set; }

    /// <summary>Gets or sets the material display name.</summary>
    public string? MaterialName { get; set; }

    /// <summary>Gets or sets the material SKU/code used for pricing and quotation handoff.</summary>
    public string? MaterialCode { get; set; }

    /// <summary>Gets or sets the ordered quantity.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Gets or sets the surface finish specification.</summary>
    public string? Finish { get; set; }

    /// <summary>Gets or sets the colour specification.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the dimensional tolerance class (e.g. ISO 286 IT7).</summary>
    public string? Tolerance { get; set; }

    /// <summary>Gets or sets manufacturing notes entered for this part.</summary>
    public string? PartNotes { get; set; }

    /// <summary>Gets or sets the AI-estimated price per unit before confirmation.</summary>
    public decimal? EstimatedPrice { get; set; }

    /// <summary>Gets or sets the employee-confirmed price per unit.</summary>
    public decimal? ConfirmedPrice { get; set; }

    /// <summary>Gets or sets the AI-suggested unit price returned by ProjectService.</summary>
    public decimal? AiSuggestedPrice { get; set; }

    /// <summary>Gets or sets the employee-confirmed unit price returned by ProjectService.</summary>
    public decimal? ConfirmedUnitPrice { get; set; }

    /// <summary>Gets or sets the reason the employee overrode the AI price, if applicable.</summary>
    public string? OverrideReason { get; set; }

    /// <summary>Gets or sets the current part status (Configuring, Priced, Confirmed, etc.).</summary>
    public string Status { get; set; } = "Configuring";

    /// <summary>Gets or sets the detailed AI price breakdown, populated after calling /price.</summary>
    public ProjectPriceBreakdownDto? PriceBreakdown { get; set; }

    /// <summary>Gets or sets the signed URL for 3D model preview from UploadService.</summary>
    public string? ModelPreviewUrl { get; set; }

    /// <summary>Gets or sets the thumbnail URL returned by ProjectService.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Gets or sets the raw GCS path for the small thumbnail artifact.</summary>
    public string? ThumbnailSmallGcsPath { get; set; }

    /// <summary>Gets or sets the raw GCS path for the large thumbnail artifact.</summary>
    public string? ThumbnailLargeGcsPath { get; set; }

    /// <summary>Gets or sets the raw GCS path for the GLB viewer artifact.</summary>
    public string? GlbStoragePath { get; set; }

    /// <summary>Gets or sets raw GCS overlay artifact paths keyed by process/category.</summary>
    public Dictionary<string, string> OverlayPaths { get; set; } = [];

    /// <summary>Gets or sets bounding box dimensions (X × Y × Z in mm).</summary>
    public ModelDimensionsDto? Dimensions { get; set; }

    /// <summary>Gets or sets whether the analyzed mesh is manifold.</summary>
    public bool? IsManifold { get; set; }

    /// <summary>CNC surface roughness code.</summary>
    public string? RoughnessCode { get; set; }

    /// <summary>Part marking type.</summary>
    public PartMarkingType MarkingType { get; set; } = PartMarkingType.None;

    /// <summary>Part marking text content.</summary>
    public string? MarkingText { get; set; }

    /// <summary>True when DFM warnings have been acknowledged.</summary>
    public bool DfmAcknowledged { get; set; }

    /// <summary>True when DFM warnings were detected for this part.</summary>
    public bool HasDfmWarnings { get; set; }

    /// <summary>True when the part requires threaded/tapped holes.</summary>
    public bool HasThreadedHoles { get; set; }

    /// <summary>Threaded hole specification.</summary>
    public string? ThreadedHoleSpec { get; set; }

    /// <summary>Number of threaded holes.</summary>
    public int ThreadedHoleCount { get; set; }

    /// <summary>True when the part requires inserts.</summary>
    public bool HasInserts { get; set; }

    /// <summary>Insert type.</summary>
    public InsertType InsertType { get; set; } = InsertType.None;

    /// <summary>Number of inserts.</summary>
    public int InsertCount { get; set; }

    /// <summary>True when the part should be individually bagged and tagged.</summary>
    public bool BagAndTag { get; set; } = true;

    /// <summary>Inspection level for quality control.</summary>
    public InspectionLevel InspectionLevel { get; set; } = InspectionLevel.Standard;

    /// <summary>Requested quality certificates.</summary>
    public List<string> Certificates { get; set; } = [];

    /// <summary>Technical drawing files attached to this part.</summary>
    public List<ProjectPartAttachmentDto> DrawingFiles { get; set; } = [];

    /// <summary>Supplementary files attached to this part.</summary>
    public List<ProjectPartAttachmentDto> SupplementaryFiles { get; set; } = [];

    /// <summary>Dynamic process configuration selections keyed by config key.</summary>
    public Dictionary<string, string> ProcessConfig { get; set; } = [];

    /// <summary>Number of mesh bodies detected in the uploaded file.</summary>
    public int? BodyCount { get; set; }

    /// <summary>Serialized body metadata from geometry analysis.</summary>
    public string? BodiesJson { get; set; }

    /// <summary>Selected body index for multi-body files.</summary>
    public int? SelectedBodyIndex { get; set; }

    /// <summary>Gets or sets the production job ID once the order is placed.</summary>
    public Guid? JobId { get; set; }

    /// <summary>Gets or sets the production job status for progress tracking.</summary>
    public string? JobStatus { get; set; }

    /// <summary>Gets or sets the production progress percentage (0–100).</summary>
    public int? JobProgressPercent { get; set; }

    /// <summary>Gets or sets the assigned machine name.</summary>
    public string? MachineName { get; set; }
}

/// <summary>
/// Attachment metadata for project part drawings and supplementary files.
/// </summary>
public class ProjectPartAttachmentDto
{
    /// <summary>Gets or sets the UploadService file ID for the attachment.</summary>
    public Guid? FileId { get; set; }

    /// <summary>Gets or sets the original file name.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw GCS storage path.</summary>
    public string? StoragePath { get; set; }

    /// <summary>Gets or sets a signed URL when resolved for the client.</summary>
    public string? SignedUrl { get; set; }

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long? SizeBytes { get; set; }

    /// <summary>Gets or sets the MIME content type.</summary>
    public string? ContentType { get; set; }

    /// <summary>Gets or sets the upload timestamp.</summary>
    public DateTime? UploadedAt { get; set; }
}

/// <summary>
/// Detailed AI-generated price breakdown for a part.
/// </summary>
public class ProjectPriceBreakdownDto
{
    /// <summary>Gets or sets the raw material cost component.</summary>
    public decimal MaterialCost { get; set; }

    /// <summary>Gets or sets the machine time cost component.</summary>
    public decimal MachineTimeCost { get; set; }

    /// <summary>Gets or sets the setup / operator cost component.</summary>
    public decimal SetupCost { get; set; }

    /// <summary>Gets or sets the complexity surcharge.</summary>
    public decimal ComplexitySurcharge { get; set; }

    /// <summary>Gets or sets the margin amount applied.</summary>
    public decimal MarginAmount { get; set; }

    /// <summary>Gets or sets the final total price per unit.</summary>
    public decimal TotalPerUnit { get; set; }

    /// <summary>Gets or sets the machine time in minutes.</summary>
    public double MachineTimeMinutes { get; set; }
}

/// <summary>
/// 3D model bounding box dimensions in millimetres.
/// </summary>
public class ModelDimensionsDto
{
    /// <summary>Gets or sets the X axis size in mm.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the Y axis size in mm.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the Z axis size in mm.</summary>
    public double Z { get; set; }
}

/// <summary>
/// A single timeline event in the project lifecycle.
/// </summary>
public class ProjectTimelineEventDto
{
    /// <summary>Gets or sets the event label (e.g. "Quotation Sent").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp of the event.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Gets or sets the MudBlazor icon for this event.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this event has occurred.</summary>
    public bool Completed { get; set; }
}

/// <summary>
/// Request to create a new project.
/// </summary>
public class CreateProjectRequest
{
    /// <summary>Gets or sets the customer this project belongs to.</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the customer name for display purposes.</summary>
    [Required, MaxLength(500)]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the project title.</summary>
    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional project description.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Gets or sets the currency code. Defaults to THB.</summary>
    [MaxLength(3)]
    public string Currency { get; set; } = "THB";

    /// <summary>Gets or sets the source project identifier when creating a duplicate/reorder draft.</summary>
    public Guid? SourceProjectId { get; set; }

    /// <summary>Gets or sets the source project number when creating a duplicate/reorder draft.</summary>
    [MaxLength(30)]
    public string? SourceProjectNumber { get; set; }
}

/// <summary>
/// Optional request payload for creating a reorder draft from an existing project.
/// </summary>
public class DuplicateProjectRequest
{
    /// <summary>Gets or sets an optional replacement title for the duplicated project.</summary>
    [MaxLength(500)]
    public string? Title { get; set; }
}

/// <summary>
/// Request to add a new part to an existing project.
/// </summary>
public class AddProjectPartRequest
{
    /// <summary>Gets or sets the uploaded file reference from UploadService.</summary>
    public Guid FileId { get; set; }

    /// <summary>Gets or sets the GCS storage path for the uploaded file.</summary>
    public string? FileReference { get; set; }

    /// <summary>Gets or sets the raw GCS path for the small thumbnail artifact.</summary>
    public string? ThumbnailSmallGcsPath { get; set; }

    /// <summary>Gets or sets the raw GCS path for the large thumbnail artifact.</summary>
    public string? ThumbnailLargeGcsPath { get; set; }

    /// <summary>Gets or sets the raw GCS path for the GLB viewer artifact.</summary>
    public string? GlbStoragePath { get; set; }

    /// <summary>Gets or sets raw GCS overlay artifact paths keyed by process/category.</summary>
    public Dictionary<string, string> OverlayPaths { get; set; } = [];

    /// <summary>Gets or sets the original filename.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the manufacturing process (FDM, SLA, CNC, etc.).</summary>
    public string? ProcessType { get; set; }

    /// <summary>Gets or sets the material ID.</summary>
    public Guid? MaterialId { get; set; }

    /// <summary>Gets or sets the denormalized material display name.</summary>
    public string? MaterialName { get; set; }

    /// <summary>Gets or sets the material SKU/code used for pricing and quotation handoff.</summary>
    public string? MaterialCode { get; set; }

    /// <summary>Gets or sets the ordered quantity.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Gets or sets optional finish specification.</summary>
    public string? Finish { get; set; }

    /// <summary>Gets or sets optional colour specification.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets optional tolerance class.</summary>
    public string? Tolerance { get; set; }

    /// <summary>Gets or sets manufacturing notes entered for this part.</summary>
    [MaxLength(2000)]
    public string? PartNotes { get; set; }

    /// <summary>CNC surface roughness code (e.g. "Ra0.8").</summary>
    public string? RoughnessCode { get; set; }

    /// <summary>Part marking type.</summary>
    public PartMarkingType MarkingType { get; set; } = PartMarkingType.None;

    /// <summary>Part marking text content.</summary>
    public string? MarkingText { get; set; }

    /// <summary>True when DFM warnings have been acknowledged.</summary>
    public bool DfmAcknowledged { get; set; }

    /// <summary>True when DFM warnings were detected for this part.</summary>
    public bool HasDfmWarnings { get; set; }

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

    /// <summary>Inspection level for quality control.</summary>
    public InspectionLevel InspectionLevel { get; set; } = InspectionLevel.Standard;

    /// <summary>Requested quality certificates.</summary>
    public List<string> Certificates { get; set; } = [];

    /// <summary>Technical drawing files attached to this part.</summary>
    public List<ProjectPartAttachmentDto> DrawingFiles { get; set; } = [];

    /// <summary>Supplementary files attached to this part.</summary>
    public List<ProjectPartAttachmentDto> SupplementaryFiles { get; set; } = [];

    /// <summary>Dynamic process configuration selections keyed by config key.</summary>
    public Dictionary<string, string> ProcessConfig { get; set; } = [];

    /// <summary>Number of mesh bodies detected in the uploaded file.</summary>
    public int? BodyCount { get; set; }

    /// <summary>Serialized body metadata from geometry analysis.</summary>
    public string? BodiesJson { get; set; }

    /// <summary>Selected body index for multi-body files.</summary>
    public int? SelectedBodyIndex { get; set; }

    /// <summary>Volume in cubic centimeters, when geometry analysis is available.</summary>
    public decimal? VolumeCm3 { get; set; }

    /// <summary>Support volume in cubic centimeters, when geometry analysis is available.</summary>
    public decimal? SupportVolumeCm3 { get; set; }

    /// <summary>Surface area in square centimeters, when geometry analysis is available.</summary>
    public decimal? SurfaceAreaCm2 { get; set; }

    /// <summary>Bounding box X dimension in millimeters.</summary>
    public decimal? BoundingBoxX { get; set; }

    /// <summary>Bounding box Y dimension in millimeters.</summary>
    public decimal? BoundingBoxY { get; set; }

    /// <summary>Bounding box Z dimension in millimeters.</summary>
    public decimal? BoundingBoxZ { get; set; }

    /// <summary>Whether the analyzed mesh is manifold.</summary>
    public bool? IsManifold { get; set; }
}

/// <summary>
/// Request to update part configuration (process, material, quantity, finish, etc.).
/// </summary>
public class UpdateProjectPartRequest
{
    /// <summary>Gets or sets the updated manufacturing process.</summary>
    public string? ProcessType { get; set; }

    /// <summary>Gets or sets the updated material ID.</summary>
    public Guid? MaterialId { get; set; }

    /// <summary>Gets or sets the updated denormalized material display name.</summary>
    public string? MaterialName { get; set; }

    /// <summary>Gets or sets the updated material SKU/code.</summary>
    public string? MaterialCode { get; set; }

    /// <summary>Gets or sets the updated quantity.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Gets or sets the updated finish.</summary>
    public string? Finish { get; set; }

    /// <summary>Gets or sets the updated colour.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the updated tolerance.</summary>
    public string? Tolerance { get; set; }

    /// <summary>Gets or sets updated manufacturing notes for this part.</summary>
    [MaxLength(2000)]
    public string? PartNotes { get; set; }

    /// <summary>CNC surface roughness code.</summary>
    public string? RoughnessCode { get; set; }

    /// <summary>Part marking type.</summary>
    public PartMarkingType MarkingType { get; set; } = PartMarkingType.None;

    /// <summary>Part marking text content.</summary>
    public string? MarkingText { get; set; }

    /// <summary>True when DFM warnings have been acknowledged.</summary>
    public bool DfmAcknowledged { get; set; }

    /// <summary>True when DFM warnings were detected for this part.</summary>
    public bool HasDfmWarnings { get; set; }

    /// <summary>True when the part requires threaded/tapped holes.</summary>
    public bool HasThreadedHoles { get; set; }

    /// <summary>Threaded hole specification.</summary>
    public string? ThreadedHoleSpec { get; set; }

    /// <summary>Number of threaded holes.</summary>
    public int ThreadedHoleCount { get; set; }

    /// <summary>True when the part requires inserts.</summary>
    public bool HasInserts { get; set; }

    /// <summary>Insert type.</summary>
    public InsertType InsertType { get; set; } = InsertType.None;

    /// <summary>Number of inserts.</summary>
    public int InsertCount { get; set; }

    /// <summary>True when the part should be individually bagged and tagged.</summary>
    public bool BagAndTag { get; set; } = true;

    /// <summary>Inspection level for quality control.</summary>
    public InspectionLevel InspectionLevel { get; set; } = InspectionLevel.Standard;

    /// <summary>Requested quality certificates.</summary>
    public List<string> Certificates { get; set; } = [];

    /// <summary>Technical drawing files attached to this part.</summary>
    public List<ProjectPartAttachmentDto> DrawingFiles { get; set; } = [];

    /// <summary>Supplementary files attached to this part.</summary>
    public List<ProjectPartAttachmentDto> SupplementaryFiles { get; set; } = [];

    /// <summary>Dynamic process configuration selections keyed by config key.</summary>
    public Dictionary<string, string> ProcessConfig { get; set; } = [];

    /// <summary>Gets or sets the raw GCS path for the small thumbnail artifact.</summary>
    public string? ThumbnailSmallGcsPath { get; set; }

    /// <summary>Gets or sets the raw GCS path for the large thumbnail artifact.</summary>
    public string? ThumbnailLargeGcsPath { get; set; }

    /// <summary>Gets or sets the raw GCS path for the GLB viewer artifact.</summary>
    public string? GlbStoragePath { get; set; }

    /// <summary>Gets or sets raw GCS overlay artifact paths keyed by process/category.</summary>
    public Dictionary<string, string> OverlayPaths { get; set; } = [];

    /// <summary>Number of mesh bodies detected in the uploaded file.</summary>
    public int? BodyCount { get; set; }

    /// <summary>Serialized body metadata from geometry analysis.</summary>
    public string? BodiesJson { get; set; }

    /// <summary>Selected body index for multi-body files.</summary>
    public int? SelectedBodyIndex { get; set; }
}

/// <summary>
/// Request to generate a quotation from confirmed project parts.
/// </summary>
public class GenerateQuotationRequest
{
    /// <summary>Gets or sets the number of days the quotation remains valid.</summary>
    [Range(1, 365)]
    public int ValidityDays { get; set; } = 30;

    /// <summary>Gets or sets delivery expectations or lead-time notes shown on the quotation.</summary>
    [MaxLength(1000)]
    public string? DeliveryExpectations { get; set; }

    /// <summary>Gets or sets the automatic bulk-order discount shown on the quotation.</summary>
    [Range(0, (double)decimal.MaxValue)]
    public decimal BulkDiscountAmount { get; set; }

    /// <summary>Gets or sets the manual discount entered for the quotation.</summary>
    [Range(0, (double)decimal.MaxValue)]
    public decimal ManualDiscountAmount { get; set; }

    /// <summary>Gets or sets the shipping or delivery cost entered for the quotation.</summary>
    [Range(0, (double)decimal.MaxValue)]
    public decimal ShippingCost { get; set; }

    /// <summary>Gets or sets the VAT or tax amount calculated for the quotation.</summary>
    [Range(0, (double)decimal.MaxValue)]
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets customer-facing quotation terms shown on the PDF.</summary>
    [MaxLength(2000)]
    public string? QuotationTerms { get; set; }

    /// <summary>Gets or sets the change summary captured on the quotation version.</summary>
    [MaxLength(1000)]
    public string? ChangeSummary { get; set; }

    /// <summary>Gets or sets an optional idempotency key for duplicate submit protection.</summary>
    [MaxLength(128)]
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Request to confirm or override the AI-estimated price for a project part.
/// </summary>
public class ConfirmPartPriceRequest
{
    /// <summary>Gets or sets the employee-confirmed price per unit.</summary>
    public decimal ConfirmedUnitPrice { get; set; }

    /// <summary>Gets or sets an optional reason when overriding the AI estimate.</summary>
    public string? PriceOverrideReason { get; set; }
}

/// <summary>
/// Stats summary for use by on the dashboard and action items panel.
/// </summary>
public class ProjectStatsDto
{
    /// <summary>Gets or sets the count of projects in any active status.</summary>
    public int ActiveCount { get; set; }

    /// <summary>Gets or sets the count of projects in Configuring status (waiting for pricing).</summary>
    public int ConfiguringCount { get; set; }

    /// <summary>Gets or sets the count of projects in Quoted status (quotation sent to customer).</summary>
    public int QuotedCount { get; set; }

    /// <summary>Gets or sets the count of projects currently In Production.</summary>
    public int InProductionCount { get; set; }
}
