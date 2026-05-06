namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Production routing information for a machine, including queue depth and planning schedule.
/// </summary>
/// <param name="MachineId">The unique identifier of the machine.</param>
/// <param name="MachineCode">The short alphanumeric code identifying the machine.</param>
/// <param name="MachineName">The human-readable name of the machine.</param>
/// <param name="QueueAhead">The number of jobs queued ahead of the current job.</param>
/// <param name="EstimatedStartDate">The estimated date and time when the current job will start.</param>
/// <param name="ScheduleItems">The ordered list of planning schedule items for this machine.</param>
/// <param name="ProposedSlotStart">The estimated start time for the proposed (not yet committed) slot for this part.</param>
/// <param name="ProposedSlotEnd">The estimated end time for the proposed slot.</param>
public record ProductionRoutingDto(
    Guid MachineId,
    string MachineCode,
    string MachineName,
    int QueueAhead,
    DateTimeOffset EstimatedStartDate,
    IReadOnlyList<PlanningScheduleItemDto> ScheduleItems,
    DateTimeOffset? ProposedSlotStart = null,
    DateTimeOffset? ProposedSlotEnd = null
);

/// <summary>
/// A single item in a machine's planning schedule.
/// </summary>
/// <param name="PlannedDate">The planned start date and time for this schedule item.</param>
/// <param name="PlannedEndDate">The planned end date and time for this schedule item.</param>
/// <param name="JobReference">The reference identifier of the job.</param>
/// <param name="Status">The scheduling status of this job. Known values: <c>Planned</c>, <c>InProgress</c>, <c>Complete</c>, <c>Cancelled</c>.</param>
/// <param name="JobId">The job unique identifier.</param>
/// <param name="MachineName">The name of the machine this job is scheduled on.</param>
/// <param name="SetupTimeMinutes">The setup time in minutes required before production.</param>
/// <param name="PrintTimeMinutes">The estimated print/machining time in minutes.</param>
/// <param name="IsProposed">True when this is an estimated (not yet committed) slot for the current part.</param>
/// <param name="IsHold">True when this item is a tentative production planning hold.</param>
/// <param name="HoldId">The planning hold identifier, if this item represents a hold.</param>
public record PlanningScheduleItemDto(
    DateTimeOffset PlannedDate,
    DateTimeOffset PlannedEndDate,
    string JobReference,
    string Status,
    Guid JobId,
    string MachineName,
    int SetupTimeMinutes,
    int PrintTimeMinutes,
    bool IsProposed = false,
    bool IsHold = false,
    Guid? HoldId = null
);
