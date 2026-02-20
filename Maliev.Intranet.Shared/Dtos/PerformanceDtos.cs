using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

public sealed record PerformanceReviewDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ReviewerId { get; set; }
    public string Cycle { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comments { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedDate { get; set; }
    public string? ReviewerName { get; set; }
}

public sealed record GoalDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int Progress { get; set; }
}

public sealed record CreateReviewRequest
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public string Cycle { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    public string Comments { get; set; } = string.Empty;
}
