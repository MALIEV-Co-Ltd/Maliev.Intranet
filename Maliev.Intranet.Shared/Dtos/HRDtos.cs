using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Employee summary information.
/// </summary>
public class EmployeeSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee number.</summary>
    public string EmployeeNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the employee's full name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Gets or sets the job title.</summary>
    public string? JobTitle { get; set; }
    /// <summary>Gets or sets the employment status.</summary>
    public string EmploymentStatus { get; set; } = string.Empty;
    /// <summary>Gets or sets the work email address.</summary>
    public string WorkEmail { get; set; } = string.Empty;
    /// <summary>Gets or sets the mobile phone number.</summary>
    public string? MobilePhone { get; set; }
    /// <summary>Gets or sets the department name.</summary>
    public string? DepartmentName { get; set; }
    /// <summary>Gets or sets the manager name.</summary>
    public string? ManagerName { get; set; }
    /// <summary>Gets or sets the hire date.</summary>
    public DateTime StartDate { get; set; }
}

/// <summary>
/// Detailed employee information.
/// </summary>
public class EmployeeDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee number.</summary>
    public string EmployeeNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the first name.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>Gets or sets the last name.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>Gets or sets the middle name.</summary>
    public string? MiddleName { get; set; }
    /// <summary>Gets or sets the full name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Gets or sets the preferred name.</summary>
    public string? PreferredName { get; set; }
    /// <summary>Gets or sets the date of birth.</summary>
    public DateTime DateOfBirth { get; set; }
    /// <summary>Gets or sets the age.</summary>
    public int? Age { get; set; }
    /// <summary>Gets or sets the nationality.</summary>
    public string? Nationality { get; set; }
    /// <summary>Gets or sets the work email address.</summary>
    public string WorkEmail { get; set; } = string.Empty;
    /// <summary>Gets or sets the personal email address.</summary>
    public string? PersonalEmail { get; set; }
    /// <summary>Gets or sets the mobile phone number.</summary>
    public string? MobilePhone { get; set; }
    /// <summary>Gets or sets the employment type.</summary>
    public string EmploymentType { get; set; } = string.Empty;
    /// <summary>Gets or sets the employment status.</summary>
    public string EmploymentStatus { get; set; } = string.Empty;
    /// <summary>Gets or sets the job title.</summary>
    public string? JobTitle { get; set; }
    /// <summary>Gets or sets the work location.</summary>
    public string? WorkLocation { get; set; }
    /// <summary>Gets or sets the hire date.</summary>
    public DateTime StartDate { get; set; }
    /// <summary>Gets or sets the probation end date.</summary>
    public DateTime? ProbationEndDate { get; set; }
    /// <summary>Gets or sets the termination date.</summary>
    public DateTime? TerminationDate { get; set; }
    /// <summary>Gets or sets the tenure in years.</summary>
    public int? TenureInYears { get; set; }
    /// <summary>Gets or sets the department ID.</summary>
    public Guid? DepartmentId { get; set; }
    /// <summary>Gets or sets the department name.</summary>
    public string? DepartmentName { get; set; }
    /// <summary>Gets or sets the manager ID.</summary>
    public Guid? ManagerId { get; set; }
    /// <summary>Gets or sets the manager name.</summary>
    public string? ManagerName { get; set; }
    /// <summary>Gets or sets the emergency contact list.</summary>
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = [];
    /// <summary>Gets or sets the collection of uploaded documents.</summary>
    public List<EmployeeDocumentDto> Documents { get; set; } = [];
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Data transfer object for an employee note.
/// </summary>
public class EmployeeNoteDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the author's name.</summary>
    public string Author { get; set; } = string.Empty;
    /// <summary>Gets or sets the note content.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Data transfer object for employee team membership.
/// </summary>
public class EmployeeTeamDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the team name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the employee's role in the team.</summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether the employee is the team lead.</summary>
    public bool IsLead { get; set; }
}

/// <summary>
/// Data transfer object for an emergency contact.
/// </summary>
public class EmergencyContactDto
{
    /// <summary>Gets or sets the contact name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the relationship to the employee.</summary>
    public string Relationship { get; set; } = string.Empty;
    /// <summary>Gets or sets the contact phone number.</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>Gets or sets the contact email.</summary>
    public string? Email { get; set; }
}

/// <summary>
/// Represents a node in the organizational chart.
/// </summary>
public class OrgNodeDto
{
    /// <summary>Gets or sets the person's name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the job title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the department.</summary>
    public string Department { get; set; } = string.Empty;
    /// <summary>Gets or sets the initials.</summary>
    public string Initials { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of direct reports.</summary>
    public int TeamCount { get; set; }
    /// <summary>Gets or sets the collection of subordinates.</summary>
    public List<OrgNodeDto> Subordinates { get; set; } = new();
}

/// <summary>
/// Statistics for compliance monitoring.
/// </summary>
public class ComplianceStatsDto
{
    /// <summary>Gets or sets the count of items expiring in 30 days.</summary>
    public int Expiring30Days { get; set; }
    /// <summary>Gets or sets the count of items expiring in 60 days.</summary>
    public int Expiring60Days { get; set; }
    /// <summary>Gets or sets the total number of active items.</summary>
    public int TotalActive { get; set; }
}

/// <summary>
/// Statistics for recruitment pipeline.
/// </summary>
public class RecruitmentStatsDto
{
    /// <summary>Gets or sets the number of applicants.</summary>
    public int Applied { get; set; }
    /// <summary>Gets or sets the number of candidates in screening.</summary>
    public int Screening { get; set; }
    /// <summary>Gets or sets the number of candidates in interview stage.</summary>
    public int Interview { get; set; }
    /// <summary>Gets or sets the number of candidates at offer stage.</summary>
    public int Offer { get; set; }
}

/// <summary>
/// Summary of a job posting.
/// </summary>
public class JobPostingSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the job title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the target department.</summary>
    public string? Department { get; set; }
    /// <summary>Gets or sets the work location.</summary>
    public string? Location { get; set; }
    /// <summary>Gets or sets the type of employment.</summary>
    public string EmploymentType { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of applicants.</summary>
    public int ApplicantCount { get; set; }
    /// <summary>Gets or sets the publication timestamp.</summary>
    public DateTime? PublishedAt { get; set; }
}

/// <summary>
/// DTO for leave balance information.
/// </summary>
public class LeaveBalanceDto
{
    /// <summary>Gets or sets the type of leave.</summary>
    public string LeaveType { get; set; } = string.Empty;
    /// <summary>Gets or sets the total entitlement.</summary>
    public decimal Entitlement { get; set; }
    /// <summary>Gets or sets the amount of leave used.</summary>
    public decimal Used { get; set; }
    /// <summary>Gets or sets the available balance.</summary>
    public decimal Available { get; set; }
}

/// <summary>
/// Summary of a leave request.
/// </summary>
public class LeaveRequestSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the type of leave.</summary>
    public string LeaveType { get; set; } = string.Empty;
    /// <summary>Gets or sets the start date.</summary>
    public DateTime StartDate { get; set; }
    /// <summary>Gets or sets the end date.</summary>
    public DateTime EndDate { get; set; }
    /// <summary>Gets or sets the duration in days.</summary>
    public decimal Days { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the name of the approver.</summary>
    public string? ApproverName { get; set; }
}

/// <summary>
/// Summary of employee compensation.
/// </summary>
public class CompensationSummaryDto
{
    /// <summary>Gets or sets the base salary.</summary>
    public decimal BaseSalary { get; set; }
    /// <summary>Gets or sets the annual bonus amount.</summary>
    public decimal AnnualBonus { get; set; }
    /// <summary>Gets or sets the total compensation.</summary>
    public decimal TotalCompensation { get; set; }
    /// <summary>Gets or sets the percentage change from previous period.</summary>
    public decimal PercentageChange { get; set; }
}

/// <summary>
/// Aggregated HR analytics data.
/// </summary>
public class HrAnalyticsDto
{
    /// <summary>Gets or sets the total employee headcount.</summary>
    public int TotalHeadcount { get; set; }
    /// <summary>Gets or sets the count of active employees.</summary>
    public int ActiveEmployees { get; set; }
    /// <summary>Gets or sets the number of employees currently in onboarding.</summary>
    public int OnboardingCount { get; set; }
    /// <summary>Gets or sets the employee turnover rate.</summary>
    public decimal TurnoverRate { get; set; }
    /// <summary>Gets or sets the distribution across departments.</summary>
    public List<DepartmentDistributionDto> DepartmentDistribution { get; set; } = [];
    /// <summary>Gets or sets the monthly hire trends.</summary>
    public List<HireTrendDto> HireTrend { get; set; } = [];
}

/// <summary>
/// Distribution of employees within a department.
/// </summary>
public class DepartmentDistributionDto
{
    /// <summary>Gets or sets the department name.</summary>
    public string Department { get; set; } = string.Empty;
    /// <summary>Gets or sets the count of employees.</summary>
    public int Count { get; set; }
    /// <summary>Gets or sets the percentage of the total headcount.</summary>
    public double Percentage { get; set; }
}

/// <summary>
/// Trend data for hiring.
/// </summary>
public class HireTrendDto
{
    /// <summary>Gets or sets the month name.</summary>
    public string Month { get; set; } = string.Empty;
    /// <summary>Gets or sets the count of hires.</summary>
    public int Count { get; set; }
}

/// <summary>
/// DTO representing an employee benefit.
/// </summary>
public class BenefitDto
{
    /// <summary>Gets or sets the benefit name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the benefit description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the status of the benefit enrollment.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the icon identifier for UI display.</summary>
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Request model for employee self-service profile update (preferred name, personal email, mobile phone).
/// </summary>
public sealed record UpdateProfileRequest
{
    /// <summary>Gets or sets the preferred name.</summary>
    public string? PreferredName { get; set; }
    /// <summary>Gets or sets the personal email address.</summary>
    public string? PersonalEmail { get; set; }
    /// <summary>Gets or sets the mobile phone number.</summary>
    public string? MobilePhone { get; set; }
}

/// <summary>
/// Request model for creating a new employee.
/// </summary>
public sealed record CreateEmployeeRequest
{
    /// <summary>Gets or sets the first name.</summary>
    [Required]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the last name.</summary>
    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the corporate email address.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the target department.</summary>
    [Required]
    public string Department { get; set; } = string.Empty;

    /// <summary>Gets or sets the job title.</summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the starting date.</summary>
    public DateTime StartDate { get; set; }
}

/// <summary>
/// Request model for updating an existing employee.
/// </summary>
public sealed record UpdateEmployeeRequest
{
    /// <summary>Gets or sets the updated department.</summary>
    public string? Department { get; set; }
    /// <summary>Gets or sets the updated job title.</summary>
    public string? Title { get; set; }
    /// <summary>Gets or sets the updated employment status.</summary>
    public string? Status { get; set; }
    /// <summary>Gets or sets the updated phone number.</summary>
    public string? Phone { get; set; }
}

/// <summary>
/// Request model for terminating an employee's contract.
/// </summary>
public sealed record TerminateEmployeeRequest
{
    /// <summary>Gets or sets the effective termination date.</summary>
    [Required]
    public DateTime TerminationDate { get; set; }

    /// <summary>Gets or sets the reason for termination.</summary>
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

/// <summary>
/// Request model for creating a new job posting.
/// </summary>
public sealed record CreateJobPostingRequest
{
    /// <summary>Gets or sets the job title.</summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the department.</summary>
    public string? Department { get; set; }
    /// <summary>Gets or sets the work location.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets the employment type (e.g., FullTime).</summary>
    [Required]
    public string EmploymentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the job description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request model for updating an existing job posting.
/// </summary>
public sealed record UpdateJobPostingRequest
{
    /// <summary>Gets or sets the job title.</summary>
    public string? Title { get; set; }
    /// <summary>Gets or sets the department.</summary>
    public string? Department { get; set; }
    /// <summary>Gets or sets the work location.</summary>
    public string? Location { get; set; }
    /// <summary>Gets or sets the employment type.</summary>
    public string? EmploymentType { get; set; }
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the posting status.</summary>
    public string? Status { get; set; }
}

/// <summary>
/// Request model for adding a candidate to a job posting.
/// </summary>
public sealed record CreateCandidateRequest
{
    /// <summary>Gets or sets the associated job posting ID.</summary>
    [Required]
    public Guid JobPostingId { get; set; }

    /// <summary>Gets or sets the candidate's full name.</summary>
    [Required]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the candidate's email address.</summary>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the candidate's phone number.</summary>
    public string? Phone { get; set; }
}

/// <summary>
/// Request model for updating a candidate's application status.
/// </summary>
public sealed record UpdateCandidateStatusRequest
{
    /// <summary>Gets or sets the new status (e.g., Interview, Offer).</summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
}
