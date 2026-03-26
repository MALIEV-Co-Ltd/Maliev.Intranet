using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for a performance review record.
/// </summary>
public sealed record PerformanceReviewDto
{
    /// <summary>The unique identifier of the performance review record.</summary>
    public Guid Id { get; set; }
    /// <summary>The identifier of the employee being reviewed.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>The identifier of the person conducting the review.</summary>
    public Guid ReviewerId { get; set; }
    /// <summary>The period or cycle the review covers (e.g., "2024-Q1", "Annual-2023").</summary>
    public string Cycle { get; set; } = string.Empty;
    /// <summary>The numerical rating assigned to the employee's performance (typically 1-5).</summary>
    public int Rating { get; set; }
    /// <summary>General feedback and comments provided by the reviewer.</summary>
    public string Comments { get; set; } = string.Empty;
    /// <summary>The current state of the review (e.g., Draft, Submitted, Completed).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The date and time when the review process was finalized.</summary>
    public DateTime? CompletedDate { get; set; }
    /// <summary>The display name of the person who conducted the review.</summary>
    public string? ReviewerName { get; set; }
}

/// <summary>
/// Data transfer object representing a specific performance goal for an employee.
/// </summary>
public sealed record GoalDto
{
    /// <summary>The unique identifier of the performance goal.</summary>
    public Guid Id { get; set; }
    /// <summary>The identifier of the employee assigned to this goal.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>A short, descriptive title for the performance goal.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>A detailed explanation of the goal's requirements and success criteria.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>The current status of the goal (e.g., NotStarted, InProgress, Achieved).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The target deadline for achieving the goal.</summary>
    public DateTime DueDate { get; set; }
    /// <summary>The current completion percentage of the goal (0-100).</summary>
    public int Progress { get; set; }
}

/// <summary>
/// Request payload for initiating a new performance review.
/// </summary>
public sealed record CreateReviewRequest
{
    /// <summary>The identifier of the employee to be reviewed.</summary>
    [Required]
    public Guid EmployeeId { get; set; }

    /// <summary>The review period or cycle (e.g., "2024-H1").</summary>
    [Required]
    public string Cycle { get; set; } = string.Empty;

    /// <summary>The initial numerical performance rating (1-5).</summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>Initial appraisal comments or feedback.</summary>
    public string Comments { get; set; } = string.Empty;
}
