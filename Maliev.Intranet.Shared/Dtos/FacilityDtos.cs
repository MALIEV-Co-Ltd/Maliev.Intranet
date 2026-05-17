using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

// ---------------------------------------------------------------------------
// Equipment
// ---------------------------------------------------------------------------

/// <summary>
/// Summary view of a piece of equipment for list endpoints.
/// </summary>
public class EquipmentSummaryDto
{
    /// <summary>Gets or sets the unique identifier of the equipment.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the auto-generated asset code in format MAL-{PREFIX}-{SEQ}.</summary>
    public string AssetCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the equipment.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the brand or manufacturer.</summary>
    public string? Brand { get; set; }

    /// <summary>Gets or sets the model name.</summary>
    public string? ModelName { get; set; }

    /// <summary>Gets or sets the equipment category as a string (e.g., "FdmPrinter", "CncMachine").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the current operational status as a string (e.g., "Active", "UnderMaintenance").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the purchase price in Thai Baht.</summary>
    public decimal? PurchasePriceTHB { get; set; }

    /// <summary>Gets or sets the next scheduled service date for planned maintenance.</summary>
    public DateOnly? NextServiceDueDate { get; set; }

    /// <summary>Gets or sets the timestamp when the equipment record was last updated (UTC).</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Full detail view of a piece of equipment, including category-specific spec data.
/// </summary>
public class EquipmentDetailDto
{
    /// <summary>Gets or sets the unique identifier of the equipment.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the auto-generated asset code in format MAL-{PREFIX}-{SEQ}.</summary>
    public string AssetCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the equipment.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the brand or manufacturer.</summary>
    public string? Brand { get; set; }

    /// <summary>Gets or sets the model name.</summary>
    public string? ModelName { get; set; }

    /// <summary>Gets or sets the equipment category as a string.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the current operational status as a string.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the purchase price in Thai Baht.</summary>
    public decimal? PurchasePriceTHB { get; set; }

    /// <summary>Gets or sets the manufacturer's serial number.</summary>
    public string? ManufacturerSerialNumber { get; set; }

    /// <summary>Gets or sets the sub-category classification.</summary>
    public string? SubCategory { get; set; }

    /// <summary>Gets or sets the date when the equipment was purchased.</summary>
    public DateOnly? PurchaseDate { get; set; }

    /// <summary>Gets or sets the warranty expiration date.</summary>
    public DateOnly? WarrantyExpiryDate { get; set; }

    /// <summary>Gets or sets the next scheduled service date.</summary>
    public DateOnly? NextServiceDueDate { get; set; }

    /// <summary>Gets or sets the timestamp when the equipment record was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the timestamp when the equipment record was last updated (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the category-specific spec properties as a key-value dictionary.
    /// Null for general equipment categories that have no manufacturing specs.
    /// </summary>
    public Dictionary<string, object?>? Spec { get; set; }
}

// ---------------------------------------------------------------------------
// Equipment Notes
// ---------------------------------------------------------------------------

/// <summary>
/// Represents an equipment note (append-only).
/// </summary>
public class EquipmentNoteDto
{
    /// <summary>Gets or sets the unique identifier of the note.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the ID of the associated equipment.</summary>
    public Guid EquipmentId { get; set; }

    /// <summary>Gets or sets the content of the note.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the employee ID of the note author.</summary>
    public Guid AuthorEmployeeId { get; set; }

    /// <summary>Gets or sets the timestamp when the note was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}

// ---------------------------------------------------------------------------
// Equipment Loans
// ---------------------------------------------------------------------------

/// <summary>
/// Represents an equipment loan record.
/// </summary>
public class EquipmentLoanDto
{
    /// <summary>Gets or sets the unique identifier of the loan record.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the ID of the loaned equipment.</summary>
    public Guid EquipmentId { get; set; }

    /// <summary>Gets or sets the asset code of the loaned equipment.</summary>
    public string AssetCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the ID of the borrower (employee or customer).</summary>
    public Guid BorrowerId { get; set; }

    /// <summary>Gets or sets the type of borrower as a string (e.g., "Employee", "Customer").</summary>
    public string BorrowerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the current loan status as a string (e.g., "Pending", "Approved", "Active", "Returned").</summary>
    public string LoanStatus { get; set; } = string.Empty;

    /// <summary>Gets or sets the start date of the loan.</summary>
    public DateOnly LoanStartDate { get; set; }

    /// <summary>Gets or sets the expected return date.</summary>
    public DateOnly ExpectedReturnDate { get; set; }

    /// <summary>Gets or sets the actual return date (null if not yet returned).</summary>
    public DateOnly? ActualReturnDate { get; set; }

    /// <summary>Gets or sets the purpose of the loan.</summary>
    public string Purpose { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Maintenance Logs
// ---------------------------------------------------------------------------

/// <summary>
/// Represents an equipment maintenance log entry.
/// </summary>
public class MaintenanceLogDto
{
    /// <summary>Gets or sets the unique identifier of the maintenance log entry.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the ID of the associated equipment.</summary>
    public Guid EquipmentId { get; set; }

    /// <summary>Gets or sets the type of maintenance performed as a string (e.g., "Preventive", "Corrective").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the maintenance work.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the date and time when the maintenance occurred (UTC).</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>Gets or sets the employee ID who logged the maintenance.</summary>
    public Guid LoggedByEmployeeId { get; set; }

    /// <summary>Gets or sets the name of the vendor who performed the maintenance.</summary>
    public string? VendorName { get; set; }

    /// <summary>Gets or sets the cost of maintenance in Thai Baht.</summary>
    public decimal? CostTHB { get; set; }

    /// <summary>Gets or sets the next scheduled service date set by this maintenance log.</summary>
    public DateOnly? NextServiceDueDate { get; set; }

    /// <summary>Gets or sets the timestamp when the log entry was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets uploaded documents, findings, photos, and reports attached to the log.</summary>
    public List<MaintenanceLogDocumentDto> Documents { get; set; } = [];
}

/// <summary>
/// Represents an uploaded document attached to an equipment maintenance log.
/// </summary>
public class MaintenanceLogDocumentDto
{
    /// <summary>Gets or sets the unique identifier of the document metadata record.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the ID of the maintenance log that owns this document.</summary>
    public Guid MaintenanceLogId { get; set; }

    /// <summary>Gets or sets the original file name shown to employees.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the MIME content type of the uploaded file.</summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets or sets the UploadService storage path or external file reference.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the document was attached to the log.</summary>
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// Metadata for a pre-uploaded maintenance document sent to FacilityService when creating a log.
/// </summary>
public class CreateMaintenanceLogDocumentDto
{
    /// <summary>Gets or sets the original file name shown to employees.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the MIME content type of the uploaded file.</summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets or sets the UploadService storage path or external file reference.</summary>
    public string StoragePath { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// CNC Attachments
// ---------------------------------------------------------------------------

/// <summary>
/// Represents a CNC machine attachment record.
/// </summary>
public class EquipmentAttachmentDto
{
    /// <summary>Gets or sets the unique identifier of the attachment.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the ID of the associated equipment.</summary>
    public Guid EquipmentId { get; set; }

    /// <summary>Gets or sets the name of the attachment.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of attachment as a string (e.g., "Tool", "Fixture", "Collet").</summary>
    public string AttachmentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the serial number of the attachment.</summary>
    public string? SerialNumber { get; set; }

    /// <summary>Gets or sets a value indicating whether the attachment is currently active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets notes on the condition of the attachment.</summary>
    public string? ConditionNotes { get; set; }

    /// <summary>Gets or sets the timestamp when the attachment record was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the timestamp when the attachment record was last updated (UTC).</summary>
    public DateTime UpdatedAt { get; set; }
}

// ---------------------------------------------------------------------------
// Request DTOs
// ---------------------------------------------------------------------------

/// <summary>
/// Request to register a new piece of equipment.
/// </summary>
public sealed record RegisterEquipmentRequest
{
    /// <summary>Gets or sets the display name of the equipment.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the equipment category (e.g., "FdmPrinter", "CncMachine", "OfficeEquipment").</summary>
    [Required]
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the brand or manufacturer.</summary>
    public string? Brand { get; set; }

    /// <summary>Gets or sets the model name.</summary>
    public string? ModelName { get; set; }

    /// <summary>Gets or sets the manufacturer's serial number.</summary>
    public string? ManufacturerSerialNumber { get; set; }

    /// <summary>Gets or sets the sub-category classification.</summary>
    public string? SubCategory { get; set; }

    /// <summary>Gets or sets the purchase price in Thai Baht.</summary>
    public decimal? PurchasePriceTHB { get; set; }

    /// <summary>Gets or sets the date of purchase.</summary>
    public DateOnly? PurchaseDate { get; set; }

    /// <summary>Gets or sets the warranty expiration date.</summary>
    public DateOnly? WarrantyExpiryDate { get; set; }

    /// <summary>Gets or sets the next scheduled service date.</summary>
    public DateOnly? NextServiceDueDate { get; set; }

    /// <summary>Gets or sets the category-specific spec properties as key-value pairs.</summary>
    public Dictionary<string, object?>? Spec { get; set; }
}

/// <summary>
/// Request to update an existing piece of equipment.
/// Requires the current xmin row version for optimistic concurrency.
/// </summary>
public sealed record UpdateEquipmentRequest
{
    /// <summary>Gets or sets the display name of the equipment.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the brand or manufacturer.</summary>
    public string? Brand { get; set; }

    /// <summary>Gets or sets the model name.</summary>
    public string? ModelName { get; set; }

    /// <summary>Gets or sets the manufacturer's serial number.</summary>
    public string? ManufacturerSerialNumber { get; set; }

    /// <summary>Gets or sets the sub-category classification.</summary>
    public string? SubCategory { get; set; }

    /// <summary>Gets or sets the purchase price in Thai Baht.</summary>
    public decimal? PurchasePriceTHB { get; set; }

    /// <summary>Gets or sets the date of purchase.</summary>
    public DateOnly? PurchaseDate { get; set; }

    /// <summary>Gets or sets the warranty expiration date.</summary>
    public DateOnly? WarrantyExpiryDate { get; set; }

    /// <summary>Gets or sets the next scheduled service date.</summary>
    public DateOnly? NextServiceDueDate { get; set; }

    /// <summary>Gets or sets the category-specific spec properties as key-value pairs.</summary>
    public Dictionary<string, object?>? Spec { get; set; }

    /// <summary>Gets or sets the xmin row version for optimistic concurrency control.</summary>
    [Required]
    public uint RowVersion { get; set; }
}

/// <summary>
/// Request to change the operational status of equipment.
/// </summary>
public sealed record ChangeEquipmentStatusRequest
{
    /// <summary>Gets or sets the new status to transition to (e.g., "UnderMaintenance", "Active").</summary>
    [Required]
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional reason for the status change.</summary>
    public string? Reason { get; set; }

    /// <summary>Gets or sets the xmin row version for optimistic concurrency control.</summary>
    [Required]
    public uint RowVersion { get; set; }
}

/// <summary>
/// Request to append a note to an equipment record.
/// </summary>
public sealed record AddEquipmentNoteRequest
{
    /// <summary>Gets or sets the content of the note.</summary>
    [Required]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a new equipment loan.
/// </summary>
public sealed record CreateLoanRequest
{
    /// <summary>Gets or sets the ID of the equipment to loan.</summary>
    [Required]
    public Guid EquipmentId { get; set; }

    /// <summary>Gets or sets the ID of the borrower (employee or customer).</summary>
    [Required]
    public Guid BorrowerId { get; set; }

    /// <summary>Gets or sets the type of borrower (e.g., "Employee", "Customer").</summary>
    [Required]
    public string BorrowerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the start date of the loan.</summary>
    [Required]
    public DateOnly LoanStartDate { get; set; }

    /// <summary>Gets or sets the expected return date.</summary>
    [Required]
    public DateOnly ExpectedReturnDate { get; set; }

    /// <summary>Gets or sets the purpose of the loan.</summary>
    [Required]
    public string Purpose { get; set; } = string.Empty;
}

/// <summary>
/// Request to approve a pending loan.
/// </summary>
public sealed record ApproveLoanRequest
{
    /// <summary>Gets or sets the ID of the employee approving the loan.</summary>
    [Required]
    public Guid ApprovedByEmployeeId { get; set; }

    /// <summary>Gets or sets the display name of the borrower, used in the loan PDF document.</summary>
    [Required]
    public string BorrowerDisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the xmin row version for optimistic concurrency control.</summary>
    [Required]
    public uint RowVersion { get; set; }
}

/// <summary>
/// Request to reject a pending loan.
/// </summary>
public sealed record RejectLoanRequest
{
    /// <summary>Gets or sets the optional reason for rejection.</summary>
    public string? Reason { get; set; }

    /// <summary>Gets or sets the xmin row version for optimistic concurrency control.</summary>
    [Required]
    public uint RowVersion { get; set; }
}

/// <summary>
/// Request to record the return of equipment from a loan.
/// </summary>
public sealed record ReturnLoanRequest
{
    /// <summary>Gets or sets the actual date of return.</summary>
    [Required]
    public DateOnly ActualReturnDate { get; set; }

    /// <summary>Gets or sets the xmin row version for optimistic concurrency control.</summary>
    [Required]
    public uint RowVersion { get; set; }
}

/// <summary>
/// Request to add a maintenance log entry.
/// </summary>
public sealed record AddMaintenanceLogRequest
{
    /// <summary>Gets or sets the type of maintenance performed (e.g., "Preventive", "Corrective", "Calibration").</summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the maintenance work.</summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the date and time when the maintenance occurred (UTC).</summary>
    [Required]
    public DateTime OccurredAt { get; set; }

    /// <summary>Gets or sets the name of the vendor who performed the maintenance.</summary>
    public string? VendorName { get; set; }

    /// <summary>Gets or sets the cost of maintenance in Thai Baht.</summary>
    public decimal? CostTHB { get; set; }

    /// <summary>Gets or sets the next scheduled service date.</summary>
    public DateOnly? NextServiceDueDate { get; set; }

    /// <summary>Gets or sets pre-uploaded maintenance document metadata.</summary>
    public List<CreateMaintenanceLogDocumentDto> Documents { get; set; } = [];
}

/// <summary>
/// Request to add a CNC attachment to equipment.
/// </summary>
public sealed record AddAttachmentRequest
{
    /// <summary>Gets or sets the name of the attachment.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of attachment (e.g., "Tool", "Fixture", "Collet", "Chuck").</summary>
    [Required]
    public string AttachmentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the serial number of the attachment.</summary>
    public string? SerialNumber { get; set; }

    /// <summary>Gets or sets notes on the condition of the attachment.</summary>
    public string? ConditionNotes { get; set; }
}

/// <summary>
/// Request to update an existing CNC attachment.
/// </summary>
public sealed record UpdateAttachmentRequest
{
    /// <summary>Gets or sets the new name of the attachment.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the new serial number.</summary>
    public string? SerialNumber { get; set; }

    /// <summary>Gets or sets a value indicating whether the attachment is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets updated condition notes.</summary>
    public string? ConditionNotes { get; set; }

    /// <summary>Gets or sets the xmin row version for optimistic concurrency control.</summary>
    [Required]
    public uint RowVersion { get; set; }
}
