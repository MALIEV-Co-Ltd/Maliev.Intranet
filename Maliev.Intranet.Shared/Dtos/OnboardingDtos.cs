using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

public sealed record OnboardingSummaryDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int Progress { get; set; }
    public string Buddy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed record OnboardingTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public Guid? AssignedTo { get; set; }
}

public sealed record OnboardingChecklistDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public List<OnboardingTaskDto> Tasks { get; set; } = [];
}

public sealed record UpdateOnboardingProgressRequest
{
    [Required]
    public Guid TaskId { get; set; }

    [Required]
    public bool IsCompleted { get; set; }
}
