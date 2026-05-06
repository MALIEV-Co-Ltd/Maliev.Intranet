namespace Maliev.Intranet.Shared.Dtos;

// ── Job DTOs ──────────────────────────────────────────────────────────────────

/// <summary>Summary data for displaying a job in the Kanban board or list view.</summary>
public class JobSummaryDto
{
    /// <summary>Gets or sets the job ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable job number (e.g. "JOB-1042").</summary>
    public string JobNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the linked order ID.</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Gets or sets the linked order number.</summary>
    public string? OrderNumber { get; set; }

    /// <summary>Gets or sets the part or product description.</summary>
    public string PartDescription { get; set; } = string.Empty;

    /// <summary>Gets or sets the manufacturing process type (e.g. "FDM", "CNC", "SLA").</summary>
    public string ProcessType { get; set; } = string.Empty;

    /// <summary>Gets or sets the material name.</summary>
    public string? Material { get; set; }

    /// <summary>Gets or sets the job priority: "Urgent", "High", "Normal", or "Low".</summary>
    public string Priority { get; set; } = "Normal";

    /// <summary>Gets or sets the current job status (e.g. "Queued", "Preparing", "InProgress", "QualityCheck", "Packaging", "Complete").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the assigned machine name.</summary>
    public string? MachineName { get; set; }

    /// <summary>Gets or sets the assigned machine ID.</summary>
    public Guid? MachineId { get; set; }

    /// <summary>Gets or sets the estimated completion date/time.</summary>
    public DateTime? EstimatedCompletionAt { get; set; }

    /// <summary>Gets or sets the job completion progress (0–100).</summary>
    public int ProgressPercent { get; set; }

    /// <summary>Gets or sets the number of units to produce.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets when the job was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the scheduled start time for this job.</summary>
    public DateTime? ScheduledStartTime { get; set; }

    /// <summary>Gets or sets the scheduled end time for this job.</summary>
    public DateTime? ScheduledEndTime { get; set; }

    /// <summary>Gets or sets the position of this job in the machine queue.</summary>
    public int QueuePosition { get; set; }

    /// <summary>Gets a value indicating whether this job is overdue.</summary>
    public bool IsOverdue => EstimatedCompletionAt.HasValue
        && EstimatedCompletionAt.Value < DateTime.UtcNow
        && Status is not "Complete";
}

/// <summary>Detailed job information including timeline and checklist.</summary>
public class JobDetailDto : JobSummaryDto
{
    /// <summary>Gets or sets the assigned operator/employee name.</summary>
    public string? AssignedTo { get; set; }

    /// <summary>Gets or sets optional job notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the job timeline entries.</summary>
    public List<JobTimelineEntryDto> Timeline { get; set; } = new();

    /// <summary>Gets or sets when the job was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    // ── 6-sided part previews for the job ticket (resolved to signed URLs by BFF) ──

    /// <summary>Signed URL for the front-face small preview. Null until GeometryService processes the file.</summary>
    public string? PreviewFrontUrl { get; set; }

    /// <summary>Signed URL for the back-face small preview.</summary>
    public string? PreviewBackUrl { get; set; }

    /// <summary>Signed URL for the left-face small preview.</summary>
    public string? PreviewLeftUrl { get; set; }

    /// <summary>Signed URL for the right-face small preview.</summary>
    public string? PreviewRightUrl { get; set; }

    /// <summary>Signed URL for the top-face small preview.</summary>
    public string? PreviewTopUrl { get; set; }

    /// <summary>Signed URL for the bottom-face small preview.</summary>
    public string? PreviewBottomUrl { get; set; }
}

/// <summary>A single timeline entry for a job status transition.</summary>
public class JobTimelineEntryDto
{
    /// <summary>Gets or sets the status at this point.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional note for the transition.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets who triggered the transition.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Gets or sets when the transition occurred.</summary>
    public DateTime Timestamp { get; set; }
}

// ── Production Queue ──────────────────────────────────────────────────────────

/// <summary>
/// Top-level production queue view containing all jobs grouped by status,
/// plus live statistics used by the stats row.
/// </summary>
public class ProductionQueueDto
{
    /// <summary>Gets or sets aggregate production statistics.</summary>
    public JobStatsDto Stats { get; set; } = new();

    /// <summary>Gets or sets all jobs in the queue, regardless of status.</summary>
    public List<JobSummaryDto> Jobs { get; set; } = new();
}

/// <summary>Aggregate job statistics for the production queue stats row.</summary>
public class JobStatsDto
{
    /// <summary>Gets or sets the number of jobs currently in Queued status.</summary>
    public int QueuedCount { get; set; }

    /// <summary>Gets or sets the number of jobs currently in progress (Preparing + InProgress + QualityCheck + Packaging).</summary>
    public int InProgressCount { get; set; }

    /// <summary>Gets or sets the number of jobs completed today.</summary>
    public int CompletedTodayCount { get; set; }

    /// <summary>Gets or sets the number of overdue jobs.</summary>
    public int OverdueCount { get; set; }

    /// <summary>Gets or sets the machine utilization percentage (0–100).</summary>
    public double MachineUtilizationPercent { get; set; }
}

// ── QR Code ───────────────────────────────────────────────────────────────────

/// <summary>QR code data for a job ticket.</summary>
public class JobQrDto
{
    /// <summary>Gets or sets the job ID.</summary>
    public Guid JobId { get; set; }

    /// <summary>Gets or sets the URL encoded in the QR code (e.g. https://intranet.maliev.com/mfg/jobs/{id}).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the base-64 encoded PNG of the QR code image.</summary>
    public string QrPngBase64 { get; set; } = string.Empty;
}

// ── Request models ────────────────────────────────────────────────────────────

/// <summary>Request model for updating a job's status.</summary>
public sealed record UpdateJobStatusRequest
{
    /// <summary>Gets or sets the new status.</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Request model for assigning a machine to a job.</summary>
public sealed record AssignMachineRequest
{
    /// <summary>Gets or sets the machine ID to assign.</summary>
    public Guid MachineId { get; set; }
}

/// <summary>Request model for reordering a job in the machine queue.</summary>
public sealed record ReorderJobRequest(int NewPosition);

/// <summary>All-machine schedule summary for Gantt planning view.</summary>
public record MachineScheduleSummaryDto(
    string MachineId,
    string MachineName,
    string Category,
    IReadOnlyList<PlanningScheduleItemDto> Schedule
);

/// <summary>A compact scheduling slot returned by the machine schedule endpoint.</summary>
public sealed record MachineScheduleItemDto(
    Guid JobId,
    string Technology,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    int SetupMinutes,
    int PrintMinutes,
    int QueuePosition,
    string Status,
    Guid OrderId,
    bool IsHold = false,
    Guid? HoldId = null,
    Guid? ProjectId = null,
    Guid? ProjectPartId = null,
    DateTime? ExpiresAt = null
);

/// <summary>Response DTO for a tentative production planning hold.</summary>
public sealed class ProductionPlanningHoldDto
{
    /// <summary>Gets or sets the hold identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the source project identifier.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the source project part identifier.</summary>
    public Guid ProjectPartId { get; set; }

    /// <summary>Gets or sets the manufacturing technology or process code.</summary>
    public string Technology { get; set; } = string.Empty;

    /// <summary>Gets or sets the reserved machine identifier or asset code.</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>Gets or sets the reserved machine display name.</summary>
    public string? MachineName { get; set; }

    /// <summary>Gets or sets the reserved queue position.</summary>
    public int QueuePosition { get; set; }

    /// <summary>Gets or sets the scheduled start time in UTC.</summary>
    public DateTime ScheduledStartTime { get; set; }

    /// <summary>Gets or sets the scheduled end time in UTC.</summary>
    public DateTime ScheduledEndTime { get; set; }

    /// <summary>Gets or sets the setup time in minutes.</summary>
    public int SetupTimeMinutes { get; set; }

    /// <summary>Gets or sets the production run time in minutes.</summary>
    public int ProductionTimeMinutes { get; set; }

    /// <summary>Gets or sets the quoted quantity covered by the hold.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the lifecycle status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets optional planning notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the user that created the hold.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the creation timestamp in UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp in UTC.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets the automatic expiration timestamp in UTC.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Gets or sets the converted job identifier, if any.</summary>
    public Guid? ConvertedJobId { get; set; }
}

/// <summary>Request DTO for creating a tentative production planning hold.</summary>
public sealed class CreateProductionPlanningHoldRequest
{
    /// <summary>Gets or sets the source project identifier.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the source project part identifier.</summary>
    public Guid ProjectPartId { get; set; }

    /// <summary>Gets or sets the manufacturing technology or process code.</summary>
    public string Technology { get; set; } = string.Empty;

    /// <summary>Gets or sets the target machine identifier or asset code.</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>Gets or sets the target machine display name.</summary>
    public string? MachineName { get; set; }

    /// <summary>Gets or sets the requested queue position. Zero appends to the queue.</summary>
    public int QueuePosition { get; set; }

    /// <summary>Gets or sets the scheduled start time in UTC.</summary>
    public DateTime ScheduledStartTime { get; set; }

    /// <summary>Gets or sets the scheduled end time in UTC.</summary>
    public DateTime? ScheduledEndTime { get; set; }

    /// <summary>Gets or sets the setup time in minutes.</summary>
    public int SetupTimeMinutes { get; set; }

    /// <summary>Gets or sets the production run time in minutes.</summary>
    public int ProductionTimeMinutes { get; set; }

    /// <summary>Gets or sets the quoted quantity covered by this hold.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets optional planning notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the automatic expiration timestamp in UTC.</summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>Request DTO for updating a tentative production planning hold.</summary>
public sealed class UpdateProductionPlanningHoldRequest
{
    /// <summary>Gets or sets the target machine identifier or asset code.</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>Gets or sets the target machine display name.</summary>
    public string? MachineName { get; set; }

    /// <summary>Gets or sets the requested queue position. Zero appends to the queue.</summary>
    public int QueuePosition { get; set; }

    /// <summary>Gets or sets the scheduled start time in UTC.</summary>
    public DateTime ScheduledStartTime { get; set; }

    /// <summary>Gets or sets the scheduled end time in UTC.</summary>
    public DateTime? ScheduledEndTime { get; set; }

    /// <summary>Gets or sets the setup time in minutes.</summary>
    public int SetupTimeMinutes { get; set; }

    /// <summary>Gets or sets the production run time in minutes.</summary>
    public int ProductionTimeMinutes { get; set; }

    /// <summary>Gets or sets optional planning notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the automatic expiration timestamp in UTC.</summary>
    public DateTime ExpiresAt { get; set; }
}
