using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Employee summary information.
/// </summary>
public class EmployeeSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; }
    public string? Manager { get; set; }
}

/// <summary>
/// Detailed employee information.
/// </summary>
public class EmployeeDetailDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Name => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? WorkLocation { get; set; }
    public string? EmployeeType { get; set; } // FullTime, PartTime, Contractor
    public List<EmployeeNoteDto> Notes { get; set; } = [];
    public List<EmployeeTeamDto> Teams { get; set; } = [];
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = [];
    public List<EmploymentHistoryDto> EmploymentHistory { get; set; } = [];
    public List<EmployeeDocumentDto> Documents { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class EmployeeNoteDto
{
    public Guid Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class EmployeeTeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsLead { get; set; }
}

public class EmergencyContactDto
{
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
}

/// <summary>
/// Represents a node in the organizational chart.
/// </summary>
public class OrgNodeDto
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public int TeamCount { get; set; }
    public List<OrgNodeDto> Subordinates { get; set; } = new();
}

/// <summary>
/// Statistics for compliance monitoring.
/// </summary>
public class ComplianceStatsDto
{
    public int Expiring30Days { get; set; }
    public int Expiring60Days { get; set; }
    public int TotalActive { get; set; }
}

/// <summary>
/// Statistics for recruitment pipeline.
/// </summary>
public class RecruitmentStatsDto
{
    public int Applied { get; set; }
    public int Screening { get; set; }
    public int Interview { get; set; }
    public int Offer { get; set; }
}

/// <summary>
/// Summary of a job posting.
/// </summary>
public class JobPostingSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Location { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public int ApplicantCount { get; set; }
    public DateTime? PublishedAt { get; set; }
}

/// <summary>
/// DTO for leave balance information.
/// </summary>
public class LeaveBalanceDto
{
    public string LeaveType { get; set; } = string.Empty;
    public decimal Entitlement { get; set; }
    public decimal Used { get; set; }
    public decimal Available { get; set; }
}

/// <summary>
/// Summary of a leave request.
/// </summary>
public class LeaveRequestSummaryDto
{
    public Guid Id { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Days { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ApproverName { get; set; }
}

/// <summary>
/// Summary of employee compensation.
/// </summary>
public class CompensationSummaryDto
{
    public decimal BaseSalary { get; set; }
    public decimal AnnualBonus { get; set; }
    public decimal TotalCompensation { get; set; }
    public decimal PercentageChange { get; set; }
}

public class HrAnalyticsDto
{
    public int TotalHeadcount { get; set; }
    public int ActiveEmployees { get; set; }
    public int OnboardingCount { get; set; }
    public decimal TurnoverRate { get; set; }
    public List<DepartmentDistributionDto> DepartmentDistribution { get; set; } = [];
    public List<HireTrendDto> HireTrend { get; set; } = [];
}

public class DepartmentDistributionDto
{
    public string Department { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class HireTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// DTO representing an employee benefit.
/// </summary>
public class BenefitDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public sealed record CreateEmployeeRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Department { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
}

public sealed record UpdateEmployeeRequest
{
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? Status { get; set; }
    public string? Phone { get; set; }
}

public sealed record TerminateEmployeeRequest
{
    [Required]
    public DateTime TerminationDate { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request model for adding an HR note to an employee record.
/// </summary>
public sealed record AddEmployeeNoteRequest
{
    /// <summary>
    /// The note content to add.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Represents a previous employment role in an employee's history.
/// </summary>
public class EmploymentHistoryDto
{
    /// <summary>Gets or sets the start date of the role.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Gets or sets the end date of the role, or null if still current.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Gets or sets the position held.</summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>Gets or sets the department for the role.</summary>
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// Represents an uploaded document linked to an employee.
/// </summary>
public class EmployeeDocumentDto
{
    /// <summary>Gets or sets the document ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the file name.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of document (e.g., Contract, ID, Certificate).</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Gets or sets when the document was uploaded.</summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>Gets or sets who uploaded the document.</summary>
    public string UploadedBy { get; set; } = string.Empty;
}

public sealed record CreateJobPostingRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Department { get; set; }
    public string? Location { get; set; }

    [Required]
    public string EmploymentType { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public sealed record UpdateJobPostingRequest
{
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Location { get; set; }
    public string? EmploymentType { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
}

public sealed record CreateCandidateRequest
{
    [Required]
    public Guid JobPostingId { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }
}

public sealed record UpdateCandidateStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
