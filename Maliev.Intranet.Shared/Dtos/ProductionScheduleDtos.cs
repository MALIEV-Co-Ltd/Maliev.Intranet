namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Full machine schedule board for production planning.
/// </summary>
public sealed class ProductionScheduleBoardDto
{
    /// <summary>Gets or sets the visible range start in UTC.</summary>
    public DateTime RangeStart { get; set; }

    /// <summary>Gets or sets the visible range end in UTC.</summary>
    public DateTime RangeEnd { get; set; }

    /// <summary>Gets or sets the machines included in the board.</summary>
    public List<ProductionScheduleMachineDto> Machines { get; set; } = [];
}

/// <summary>
/// Machine row in a production schedule board.
/// </summary>
public sealed class ProductionScheduleMachineDto
{
    /// <summary>Gets or sets the machine identifier or asset code.</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>Gets or sets the machine display name.</summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>Gets or sets the FacilityService equipment category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the default manufacturing technology for the machine row.</summary>
    public string Technology { get; set; } = string.Empty;

    /// <summary>Gets or sets the schedule slots for this machine.</summary>
    public List<ProductionScheduleSlotDto> Slots { get; set; } = [];
}

/// <summary>
/// A job, planning hold, or proposed slot displayed on the production schedule board.
/// </summary>
public sealed class ProductionScheduleSlotDto
{
    /// <summary>Gets or sets the stable slot identifier.</summary>
    public Guid SlotId { get; set; }

    /// <summary>Gets or sets the production job identifier when this slot is a job.</summary>
    public Guid? JobId { get; set; }

    /// <summary>Gets or sets the planning hold identifier when this slot is a hold.</summary>
    public Guid? HoldId { get; set; }

    /// <summary>Gets or sets the related project identifier when available.</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>Gets or sets the related project part identifier when available.</summary>
    public Guid? ProjectPartId { get; set; }

    /// <summary>Gets or sets the source part file name when available.</summary>
    public string? FileName { get; set; }

    /// <summary>Gets or sets the scheduled machine identifier or asset code.</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduled machine display name.</summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>Gets or sets the manufacturing technology or process code.</summary>
    public string Technology { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduled start time in UTC.</summary>
    public DateTime ScheduledStart { get; set; }

    /// <summary>Gets or sets the scheduled end time in UTC.</summary>
    public DateTime ScheduledEnd { get; set; }

    /// <summary>Gets or sets the setup time in minutes.</summary>
    public int SetupMinutes { get; set; }

    /// <summary>Gets or sets the production time in minutes.</summary>
    public int ProductionMinutes { get; set; }

    /// <summary>Gets or sets the queue position on the machine.</summary>
    public int QueuePosition { get; set; }

    /// <summary>Gets or sets the status label.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the display label shown inside the schedule bar.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the planning hold expiration timestamp when applicable.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets a value indicating whether this slot is a tentative planning hold.</summary>
    public bool IsHold { get; set; }

    /// <summary>Gets or sets a value indicating whether this slot is a calculated proposal.</summary>
    public bool IsProposed { get; set; }

    /// <summary>Gets or sets a value indicating whether this slot belongs to the current project context.</summary>
    public bool IsCurrentProject { get; set; }

    /// <summary>Gets or sets a value indicating whether the UI may move this slot.</summary>
    public bool CanMove { get; set; }
}

/// <summary>
/// Describes a schedule slot move requested from the visual production board.
/// </summary>
public sealed record ProductionScheduleSlotMoveRequest
{
    /// <summary>Gets the moved schedule slot.</summary>
    public required ProductionScheduleSlotDto Slot { get; init; }

    /// <summary>Gets the target machine identifier or asset code.</summary>
    public required string MachineId { get; init; }

    /// <summary>Gets the target machine display name.</summary>
    public string? MachineName { get; init; }

    /// <summary>Gets the requested scheduled start time in UTC.</summary>
    public required DateTime ScheduledStart { get; init; }

    /// <summary>Gets the requested scheduled end time in UTC.</summary>
    public required DateTime ScheduledEnd { get; init; }
}

/// <summary>
/// Request model for moving a queued job to a schedule slot.
/// </summary>
public sealed record RescheduleJobRequest
{
    /// <summary>Gets or sets the target machine identifier or asset code.</summary>
    public string MachineId { get; init; } = string.Empty;

    /// <summary>Gets or sets the scheduled start time in UTC.</summary>
    public DateTime ScheduledStartTime { get; init; }

    /// <summary>Gets or sets the scheduled end time in UTC.</summary>
    public DateTime? ScheduledEndTime { get; init; }

    /// <summary>Gets or sets the optional queue position.</summary>
    public int? QueuePosition { get; init; }

    /// <summary>Gets or sets a value indicating whether following queued jobs should be compacted after the move.</summary>
    public bool CascadeFollowingJobs { get; init; }
}
