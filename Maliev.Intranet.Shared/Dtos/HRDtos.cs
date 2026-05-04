using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Employee summary information for high-level directory and selection lists.
/// </summary>
public class EmployeeSummaryDto
{
    /// <summary>The unique identifier for the employee record.</summary>
    public Guid Id { get; set; }
    /// <summary>The full display name of the employee.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The primary work email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The department the employee is currently assigned to.</summary>
    public string Department { get; set; } = string.Empty;
    /// <summary>The employee's official job title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>The user's primary IAM/business role display name.</summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>The primary contact phone number.</summary>
    public string? Phone { get; set; }
    /// <summary>The current employment status (e.g., Active, OnLeave, Terminated).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The date when the employee officially joined the company.</summary>
    public DateTime? HireDate { get; set; }
    /// <summary>The display name of the employee's direct manager.</summary>
    public string? Manager { get; set; }
}

/// <summary>
/// Comprehensive employee information for detailed profile management.
/// </summary>
public class EmployeeDetailDto
{
    /// <summary>The unique identifier for the employee record.</summary>
    public Guid Id { get; set; }
    /// <summary>The legal first name of the employee.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>The legal last name of the employee.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>The full concatenated name of the employee.</summary>
    public string Name => $"{FirstName} {LastName}";
    /// <summary>The primary work email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The primary contact phone number.</summary>
    public string? Phone { get; set; }
    /// <summary>The department the employee belongs to.</summary>
    public string Department { get; set; } = string.Empty;
    /// <summary>The employee's official job title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>The user's primary IAM/business role display name.</summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>The current employment status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The unique identifier of the employee's direct manager.</summary>
    public string? ManagerId { get; set; }
    /// <summary>The name of the employee's direct manager.</summary>
    public string? ManagerName { get; set; }
    /// <summary>The date when the employee was hired.</summary>
    public DateTime? HireDate { get; set; }
    /// <summary>The date when employment was terminated, if applicable.</summary>
    public DateTime? TerminationDate { get; set; }
    /// <summary>The primary work location (e.g., Office, Remote).</summary>
    public string? WorkLocation { get; set; }
    /// <summary>The type of employment contract (e.g., FullTime, PartTime, Contractor).</summary>
    public string? EmployeeType { get; set; }
    /// <summary>List of internal notes and feedback related to the employee.</summary>
    public List<EmployeeNoteDto> Notes { get; set; } = [];
    /// <summary>List of teams the employee is currently a member of.</summary>
    public List<EmployeeTeamDto> Teams { get; set; } = [];
    /// <summary>Emergency contact details for the employee.</summary>
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = [];
    /// <summary>Historical record of internal roles and positions held by the employee.</summary>
    public List<EmploymentHistoryDto> EmploymentHistory { get; set; } = [];
    /// <summary>List of digital documents and files associated with the employee profile.</summary>
    public List<EmployeeDocumentDto> Documents { get; set; } = [];
    /// <summary>The date and time when the profile was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The date and time when the profile was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Self-service employee profile data exposed to the signed-in employee.
/// </summary>
public sealed class EmployeeSelfProfileDto
{
    /// <summary>The employee record identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>The internal employee number.</summary>
    public string EmployeeNumber { get; set; } = string.Empty;
    /// <summary>The employee's legal first name.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>The employee's legal last name.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>The employee's full display name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>The employee's preferred name or nickname.</summary>
    public string? PreferredName { get; set; }
    /// <summary>The employee's work email address.</summary>
    public string WorkEmail { get; set; } = string.Empty;
    /// <summary>The employee's personal email address.</summary>
    public string? PersonalEmail { get; set; }
    /// <summary>The employee's mobile phone number.</summary>
    public string? MobilePhone { get; set; }
    /// <summary>The employee's job title.</summary>
    public string? JobTitle { get; set; }
    /// <summary>The employee's work location.</summary>
    public string? WorkLocation { get; set; }
    /// <summary>The employee's employment type.</summary>
    public string EmploymentType { get; set; } = string.Empty;
    /// <summary>The employee's employment status.</summary>
    public string EmploymentStatus { get; set; } = string.Empty;
    /// <summary>The employee's start or hire date.</summary>
    public DateTime? StartDate { get; set; }
    /// <summary>The date and time when the employee/profile record was created.</summary>
    public DateTime? CreatedAt { get; set; }
    /// <summary>The employee's department name.</summary>
    public string? DepartmentName { get; set; }
    /// <summary>The employee's manager display name.</summary>
    public string? ManagerName { get; set; }
    /// <summary>The employee's emergency contact list.</summary>
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = [];
}

/// <summary>
/// Self-service profile update request for fields employees may edit themselves.
/// </summary>
public sealed class UpdateEmployeeSelfProfileRequest
{
    /// <summary>The employee's personal email address.</summary>
    [EmailAddress]
    [StringLength(255)]
    public string? PersonalEmail { get; set; }

    /// <summary>The employee's mobile phone number.</summary>
    [Phone]
    [StringLength(20)]
    public string? MobilePhone { get; set; }

    /// <summary>The employee's preferred name or nickname.</summary>
    [StringLength(100)]
    public string? PreferredName { get; set; }
}

/// <summary>
/// Represents an internal note or observation recorded on an employee profile.
/// </summary>
public class EmployeeNoteDto
{
    /// <summary>The unique identifier for the note.</summary>
    public Guid Id { get; set; }
    /// <summary>The name or identifier of the user who authored the note.</summary>
    public string Author { get; set; } = string.Empty;
    /// <summary>The text content of the note.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>The date and time when the note was recorded.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents an employee's membership and role within a specific team.
/// </summary>
public class EmployeeTeamDto
{
    /// <summary>The unique identifier for the team.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of the team.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The functional role the employee performs in the team.</summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>Indicates if the employee is designated as the team leader.</summary>
    public bool IsLead { get; set; }
}

/// <summary>
/// Emergency contact information for an employee.
/// </summary>
public class EmergencyContactDto
{
    /// <summary>The full name of the emergency contact person.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The personal relationship to the employee (e.g., Spouse, Parent).</summary>
    public string Relationship { get; set; } = string.Empty;
    /// <summary>The contact phone number for emergencies.</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>The contact email address, if available.</summary>
    public string? Email { get; set; }
}

/// <summary>
/// Represents a node in the organizational chart, including hierarchical relationships.
/// </summary>
public class OrgNodeDto
{
    /// <summary>The display name of the employee at this node.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The official job title of the employee.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>The department the employee belongs to.</summary>
    public string Department { get; set; } = string.Empty;
    /// <summary>The name initials used for avatars and compact views.</summary>
    public string Initials { get; set; } = string.Empty;
    /// <summary>The number of direct reports or team members under this node.</summary>
    public int TeamCount { get; set; }
    /// <summary>List of organizational nodes representing direct reports.</summary>
    public List<OrgNodeDto> Subordinates { get; set; } = new();
}

/// <summary>
/// Statistics for monitoring compliance and expiring documents.
/// </summary>
public class ComplianceStatsDto
{
    /// <summary>The number of compliance items expiring within the next 30 days.</summary>
    public int Expiring30Days { get; set; }
    /// <summary>The number of compliance items expiring within the next 60 days.</summary>
    public int Expiring60Days { get; set; }
    /// <summary>The total number of currently active compliance items.</summary>
    public int TotalActive { get; set; }
}

/// <summary>
/// Aggregate statistics for the recruitment pipeline and candidate funnel.
/// </summary>
public class RecruitmentStatsDto
{
    /// <summary>The total number of applicants in the initial application stage.</summary>
    public int Applied { get; set; }
    /// <summary>The number of candidates currently in the screening phase.</summary>
    public int Screening { get; set; }
    /// <summary>The number of candidates currently undergoing interviews.</summary>
    public int Interview { get; set; }
    /// <summary>The number of active job offers extended to candidates.</summary>
    public int Offer { get; set; }
}

/// <summary>
/// Summary information for an active or historical job posting.
/// </summary>
public class JobPostingSummaryDto
{
    /// <summary>The unique identifier for the job posting.</summary>
    public Guid Id { get; set; }
    /// <summary>The advertised job title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>The department the role belongs to.</summary>
    public string? Department { get; set; }
    /// <summary>The primary work location for the position.</summary>
    public string? Location { get; set; }
    /// <summary>The type of employment (e.g., Permanent, Contract).</summary>
    public string EmploymentType { get; set; } = string.Empty;
    /// <summary>The number of candidates who have applied for this position.</summary>
    public int ApplicantCount { get; set; }
    /// <summary>The date and time when the posting was published.</summary>
    public DateTime? PublishedAt { get; set; }
}

/// <summary>
/// Detailed breakdown of an employee's leave entitlement and usage.
/// </summary>
public class LeaveBalanceDto
{
    /// <summary>The category of leave (e.g., Annual, Sick, Personal).</summary>
    public string LeaveType { get; set; } = string.Empty;
    /// <summary>The total number of days the employee is entitled to for this year.</summary>
    public decimal Entitlement { get; set; }
    /// <summary>The number of leave days already consumed in the current period.</summary>
    public decimal Used { get; set; }
    /// <summary>The number of remaining leave days available for use.</summary>
    public decimal Available { get; set; }
}

/// <summary>
/// Summary details for an individual leave request and its current status.
/// </summary>
public class LeaveRequestSummaryDto
{
    /// <summary>The unique identifier for the leave request.</summary>
    public Guid Id { get; set; }
    /// <summary>The category of leave being requested.</summary>
    public string LeaveType { get; set; } = string.Empty;
    /// <summary>The start date of the requested leave period.</summary>
    public DateTime StartDate { get; set; }
    /// <summary>The end date of the requested leave period.</summary>
    public DateTime EndDate { get; set; }
    /// <summary>The total number of working days requested.</summary>
    public decimal Days { get; set; }
    /// <summary>The current approval status of the request (e.g., Pending, Approved).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The name of the user who is responsible for approving the request.</summary>
    public string? ApproverName { get; set; }
}

/// <summary>
/// Summary of an employee's compensation package and periodic changes.
/// </summary>
public class CompensationSummaryDto
{
    /// <summary>The current annual base salary amount.</summary>
    public decimal BaseSalary { get; set; }
    /// <summary>The projected or historical annual bonus amount.</summary>
    public decimal AnnualBonus { get; set; }
    /// <summary>The total calculated annual compensation (base plus bonus).</summary>
    public decimal TotalCompensation { get; set; }
    /// <summary>The percentage change in total compensation compared to the previous period.</summary>
    public decimal PercentageChange { get; set; }
}

/// <summary>
/// Aggregate analytics for HR monitoring, including headcount and turnover trends.
/// </summary>
public class HrAnalyticsDto
{
    /// <summary>The total number of employee records in the system.</summary>
    public int TotalHeadcount { get; set; }
    /// <summary>The count of employees with an active employment status.</summary>
    public int ActiveEmployees { get; set; }
    /// <summary>The number of employees currently in the onboarding phase.</summary>
    public int OnboardingCount { get; set; }
    /// <summary>The calculated annual employee turnover rate percentage.</summary>
    public decimal TurnoverRate { get; set; }
    /// <summary>Distribution of employees across various company departments.</summary>
    public List<DepartmentDistributionDto> DepartmentDistribution { get; set; } = [];
    /// <summary>Historical trend of new hires over time.</summary>
    public List<HireTrendDto> HireTrend { get; set; } = [];
}

/// <summary>
/// Breakdown of headcount distribution for a specific department.
/// </summary>
public class DepartmentDistributionDto
{
    /// <summary>The name of the department.</summary>
    public string Department { get; set; } = string.Empty;
    /// <summary>The number of employees assigned to this department.</summary>
    public int Count { get; set; }
    /// <summary>The percentage of total headcount this department represents.</summary>
    public double Percentage { get; set; }
}

/// <summary>
/// Represents the number of successful hires recorded for a specific month.
/// </summary>
public class HireTrendDto
{
    /// <summary>The month name or identifier (e.g., "January 2024").</summary>
    public string Month { get; set; } = string.Empty;
    /// <summary>The number of employees who joined during this month.</summary>
    public int Count { get; set; }
}

/// <summary>
/// Represents a company-provided benefit available to an employee.
/// </summary>
public class BenefitDto
{
    /// <summary>The display name of the benefit (e.g., "Health Insurance").</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>A detailed description of what the benefit covers.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>The employee's current enrollment status for this benefit.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The CSS class or identifier for the benefit's display icon.</summary>
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Request model for creating a new employee record.
/// </summary>
public sealed record CreateEmployeeRequest
{
    /// <summary>The first name of the new employee.</summary>
    [Required]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The last name of the new employee.</summary>
    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>The primary work email address for the new employee.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>The department the employee will be assigned to.</summary>
    [Required]
    public string Department { get; set; } = string.Empty;

    /// <summary>The initial job title for the employee.</summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>The scheduled start date for the employee.</summary>
    public DateTime StartDate { get; set; }
}

/// <summary>
/// Request model for updating specific fields of an existing employee record.
/// </summary>
public sealed record UpdateEmployeeRequest
{
    /// <summary>The updated department name.</summary>
    public string? Department { get; set; }
    /// <summary>The updated job title.</summary>
    public string? Title { get; set; }
    /// <summary>The new employment status.</summary>
    public string? Status { get; set; }
    /// <summary>The updated contact phone number.</summary>
    public string? Phone { get; set; }
}

/// <summary>
/// Request model for processing an employee's termination.
/// </summary>
public sealed record TerminateEmployeeRequest
{
    /// <summary>The official date of termination.</summary>
    [Required]
    public DateTime TerminationDate { get; set; }

    /// <summary>The reason for the employment termination.</summary>
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
    /// <summary>The start date of the role.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>The end date of the role, or null if still current.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>The position or job title held during this period.</summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>The department for the role.</summary>
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// Represents an uploaded document linked to an employee.
/// </summary>
public class EmployeeDocumentDto
{
    /// <summary>The unique identifier for the document record.</summary>
    public Guid Id { get; set; }

    /// <summary>The display name of the file.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>The type of document (e.g., Contract, ID, Certificate).</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>The date and time when the document was uploaded.</summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>The identifier of the user who uploaded the document.</summary>
    public string UploadedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request model for creating a new job vacancy posting.
/// </summary>
public sealed record CreateJobPostingRequest
{
    /// <summary>The job title to be advertised.</summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>The department where the vacancy exists.</summary>
    public string? Department { get; set; }
    /// <summary>The primary work location for the position.</summary>
    public string? Location { get; set; }

    /// <summary>The type of employment offered (e.g., Full-Time, Internship).</summary>
    [Required]
    public string EmploymentType { get; set; } = string.Empty;

    /// <summary>A detailed description of the role and requirements.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request model for updating the details of an existing job posting.
/// </summary>
public sealed record UpdateJobPostingRequest
{
    /// <summary>The updated job title.</summary>
    public string? Title { get; set; }
    /// <summary>The updated department name.</summary>
    public string? Department { get; set; }
    /// <summary>The updated work location.</summary>
    public string? Location { get; set; }
    /// <summary>The updated employment type.</summary>
    public string? EmploymentType { get; set; }
    /// <summary>The updated job description.</summary>
    public string? Description { get; set; }
    /// <summary>The new status of the job posting (e.g., Published, Closed).</summary>
    public string? Status { get; set; }
}

/// <summary>
/// Request model for submitting a candidate's application for a job posting.
/// </summary>
public sealed record CreateCandidateRequest
{
    /// <summary>The identifier of the target job posting.</summary>
    [Required]
    public Guid JobPostingId { get; set; }

    /// <summary>The full name of the candidate.</summary>
    [Required]
    public string FullName { get; set; } = string.Empty;

    /// <summary>The candidate's primary contact email address.</summary>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>The candidate's contact phone number.</summary>
    public string? Phone { get; set; }
}

/// <summary>
/// Request model for updating the status of a candidate in the recruitment pipeline.
/// </summary>
public sealed record UpdateCandidateStatusRequest
{
    /// <summary>The new recruitment status for the candidate (e.g., Interviewing, Rejected).</summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>Optional notes or feedback regarding the status change.</summary>
    public string? Notes { get; set; }
}
