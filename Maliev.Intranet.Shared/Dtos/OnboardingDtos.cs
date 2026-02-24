using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Summary information for an onboarding process.
/// </summary>
public sealed record OnboardingSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee ID.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>Gets or sets the name of the employee.</summary>
    public string EmployeeName { get; set; } = string.Empty;
    /// <summary>Gets or sets the department name.</summary>
    public string Department { get; set; } = string.Empty;
    /// <summary>Gets or sets the start date.</summary>
    public DateTime StartDate { get; set; }
    /// <summary>Gets or sets the onboarding progress percentage.</summary>
    public int Progress { get; set; }
    /// <summary>Gets or sets the name of the assigned onboarding buddy.</summary>
    public string Buddy { get; set; } = string.Empty;
    /// <summary>Gets or sets the current status (e.g., InProgress, Completed).</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for an individual onboarding task.
/// </summary>
public sealed record OnboardingTaskDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the task title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the detailed description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether the task is completed.</summary>
    public bool IsCompleted { get; set; }
    /// <summary>Gets or sets the optional ID of the person assigned to this task.</summary>
    public Guid? AssignedTo { get; set; }
}

/// <summary>
/// Data transfer object for an onboarding checklist.
/// </summary>
public sealed record OnboardingChecklistDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee ID.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>Gets or sets the collection of tasks in the checklist.</summary>
    public List<OnboardingTaskDto> Tasks { get; set; } = [];
}

/// <summary>
/// Request model for updating onboarding progress.
/// </summary>
public sealed record UpdateOnboardingProgressRequest
{
    /// <summary>Gets or sets the identifier of the task being updated.</summary>
    [Required]
    public Guid TaskId { get; set; }

    /// <summary>Gets or sets a value indicating whether the task is now completed.</summary>
    [Required]
    public bool IsCompleted { get; set; }
}
