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
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Detailed employee information.
/// </summary>
public class EmployeeDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
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
