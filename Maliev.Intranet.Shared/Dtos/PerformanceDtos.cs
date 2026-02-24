using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for a performance review.
/// </summary>
public sealed record PerformanceReviewDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee ID.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>Gets or sets the identifier of the reviewer.</summary>
    public Guid ReviewerId { get; set; }
    /// <summary>Gets or sets the review cycle name (e.g., Annual 2025).</summary>
    public string Cycle { get; set; } = string.Empty;
    /// <summary>Gets or sets the performance rating score.</summary>
    public int Rating { get; set; }
    /// <summary>Gets or sets the review comments.</summary>
    public string Comments { get; set; } = string.Empty;
    /// <summary>Gets or sets the current status of the review.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the completion date, if applicable.</summary>
    public DateTime? CompletedDate { get; set; }
    /// <summary>Gets or sets the name of the reviewer.</summary>
    public string? ReviewerName { get; set; }
}

/// <summary>
/// Data transfer object for an employee goal.
/// </summary>
public sealed record GoalDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee ID.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>Gets or sets the goal title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the detailed description of the goal.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the current status of the goal.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the due date.</summary>
    public DateTime DueDate { get; set; }
    /// <summary>Gets or sets the current progress percentage.</summary>
    public int Progress { get; set; }
}

/// <summary>
/// Request model for creating a performance review.
/// </summary>
public sealed record CreateReviewRequest
{
    /// <summary>Gets or sets the employee ID to review.</summary>
    [Required]
    public Guid EmployeeId { get; set; }

    /// <summary>Gets or sets the review cycle.</summary>
    [Required]
    public string Cycle { get; set; } = string.Empty;

    /// <summary>Gets or sets the rating score (1-5).</summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>Gets or sets the review comments.</summary>
    public string Comments { get; set; } = string.Empty;
}
