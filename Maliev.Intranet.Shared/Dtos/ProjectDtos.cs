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

    /// <summary>Gets or sets the date the project was created.</summary>
    public DateTime CreatedAt { get; set; }
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

    /// <summary>Gets or sets the project title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description of the project scope.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the current lifecycle status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets optional internal notes about this project.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the date until which the quotation is valid.</summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>Gets or sets the total confirmed price across all parts.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>Gets or sets the currency code (e.g. THB).</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Gets or sets the associated quotation ID once generated.</summary>
    public Guid? QuotationId { get; set; }

    /// <summary>Gets or sets the quotation status once the quotation has been generated.</summary>
    public string? QuotationStatus { get; set; }

    /// <summary>Gets or sets the date the project was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the list of parts belonging to this project.</summary>
    public List<ProjectPartDto> Parts { get; set; } = new();

    /// <summary>Gets or sets the status timeline events for this project.</summary>
    public List<ProjectTimelineEventDto> Timeline { get; set; } = new();
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

    /// <summary>Gets or sets the original uploaded filename (e.g. bracket_v2.stl).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the manufacturing process type (FDM, SLA, CNC, etc.).</summary>
    public string? ProcessType { get; set; }

    /// <summary>Gets or sets the material ID.</summary>
    public Guid? MaterialId { get; set; }

    /// <summary>Gets or sets the material display name.</summary>
    public string? MaterialName { get; set; }

    /// <summary>Gets or sets the ordered quantity.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Gets or sets the surface finish specification.</summary>
    public string? Finish { get; set; }

    /// <summary>Gets or sets the colour specification.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the dimensional tolerance class (e.g. ISO 286 IT7).</summary>
    public string? Tolerance { get; set; }

    /// <summary>Gets or sets the AI-estimated price per unit before confirmation.</summary>
    public decimal? EstimatedPrice { get; set; }

    /// <summary>Gets or sets the employee-confirmed price per unit.</summary>
    public decimal? ConfirmedPrice { get; set; }

    /// <summary>Gets or sets the reason the employee overrode the AI price, if applicable.</summary>
    public string? OverrideReason { get; set; }

    /// <summary>Gets or sets the current part status (Configuring, Priced, Confirmed, etc.).</summary>
    public string Status { get; set; } = "Configuring";

    /// <summary>Gets or sets the detailed AI price breakdown, populated after calling /price.</summary>
    public ProjectPriceBreakdownDto? PriceBreakdown { get; set; }

    /// <summary>Gets or sets the signed URL for 3D model preview from UploadService.</summary>
    public string? ModelPreviewUrl { get; set; }

    /// <summary>Gets or sets bounding box dimensions (X × Y × Z in mm).</summary>
    public ModelDimensionsDto? Dimensions { get; set; }

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
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the project title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional project description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets how many days from today the quotation will be valid.</summary>
    public int ValidityDays { get; set; } = 30;
}

/// <summary>
/// Request to add a new part to an existing project.
/// </summary>
public class AddProjectPartRequest
{
    /// <summary>Gets or sets the uploaded file reference from UploadService.</summary>
    public Guid FileId { get; set; }

    /// <summary>Gets or sets the original filename.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the manufacturing process (FDM, SLA, CNC, etc.).</summary>
    public string? ProcessType { get; set; }

    /// <summary>Gets or sets the material ID.</summary>
    public Guid? MaterialId { get; set; }

    /// <summary>Gets or sets the ordered quantity.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Gets or sets optional finish specification.</summary>
    public string? Finish { get; set; }

    /// <summary>Gets or sets optional colour specification.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets optional tolerance class.</summary>
    public string? Tolerance { get; set; }
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

    /// <summary>Gets or sets the updated quantity.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Gets or sets the updated finish.</summary>
    public string? Finish { get; set; }

    /// <summary>Gets or sets the updated colour.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the updated tolerance.</summary>
    public string? Tolerance { get; set; }
}

/// <summary>
/// Request to confirm or override the AI-estimated price for a project part.
/// </summary>
public class ConfirmPartPriceRequest
{
    /// <summary>Gets or sets the employee-confirmed price per unit.</summary>
    public decimal ConfirmedPrice { get; set; }

    /// <summary>Gets or sets an optional reason when overriding the AI estimate.</summary>
    public string? OverrideReason { get; set; }
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
