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
public record ProductionRoutingDto(
    Guid MachineId,
    string MachineCode,
    string MachineName,
    int QueueAhead,
    DateTimeOffset EstimatedStartDate,
    IReadOnlyList<PlanningScheduleItemDto> ScheduleItems
);

/// <summary>
/// A single item in a machine's planning schedule.
/// </summary>
/// <param name="PlannedDate">The planned date and time for this schedule item.</param>
/// <param name="JobReference">The reference identifier of the job.</param>
/// <param name="Status">The scheduling status of this job. Known values: <c>Planned</c>, <c>InProgress</c>, <c>Complete</c>, <c>Cancelled</c>.</param>
public record PlanningScheduleItemDto(
    DateTimeOffset PlannedDate,
    string JobReference,
    string Status
);
