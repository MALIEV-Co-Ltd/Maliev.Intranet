using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Represents a summary of an employee's onboarding process.
/// </summary>
public sealed record OnboardingSummaryDto
{
    /// <summary>
    /// The unique identifier of the onboarding record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the employee being onboarded.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// The full name of the employee being onboarded.
    /// </summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// The department the employee is joining.
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// The employee's official start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The percentage of completion for the onboarding checklist.
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// The name of the assigned onboarding buddy for the employee.
    /// </summary>
    public string Buddy { get; set; } = string.Empty;

    /// <summary>
    /// The current overall status of the onboarding process (e.g., InProgress, Completed).
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a specific task within an onboarding checklist.
/// </summary>
public sealed record OnboardingTaskDto
{
    /// <summary>
    /// The unique identifier of the onboarding task.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The title or brief name of the task.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A detailed description of what needs to be done for this task.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the task has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// The unique identifier of the person or role assigned to complete this task.
    /// </summary>
    public Guid? AssignedTo { get; set; }
}

/// <summary>
/// Represents the complete onboarding checklist for an employee.
/// </summary>
public sealed record OnboardingChecklistDto
{
    /// <summary>
    /// The unique identifier of the checklist.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the employee this checklist belongs to.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// The list of tasks included in the employee's onboarding checklist.
    /// </summary>
    public List<OnboardingTaskDto> Tasks { get; set; } = [];
}

/// <summary>
/// Request to update the completion status of an onboarding task.
/// </summary>
public sealed record UpdateOnboardingProgressRequest
{
    /// <summary>
    /// The unique identifier of the task to update.
    /// </summary>
    [Required]
    public Guid TaskId { get; set; }

    /// <summary>
    /// The new completion status for the task.
    /// </summary>
    [Required]
    public bool IsCompleted { get; set; }
}
