using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for leave-related operations.
/// </summary>
public class LeaveDto
{
}

/// <summary>
/// Request to submit a leave request.
/// </summary>
public sealed record SubmitLeaveRequestDto
{
    /// <summary>Gets or sets the type of leave (e.g., Sick, Annual).</summary>
    [Required]
    public string LeaveType { get; set; } = string.Empty;

    /// <summary>Gets or sets the start date of the leave.</summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>Gets or sets the end date of the leave.</summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>Gets or sets the period for a half-day leave (e.g., Morning, Afternoon).</summary>
    public string? HalfDayPeriod { get; set; } // Morning, Afternoon, or null

    /// <summary>Gets or sets the reason for the leave request.</summary>
    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response for a leave request detail view.
/// </summary>
public sealed record LeaveRequestDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee ID.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>Gets or sets the employee name.</summary>
    public string EmployeeName { get; set; } = string.Empty;
    /// <summary>Gets or sets the leave type.</summary>
    public string LeaveType { get; set; } = string.Empty;
    /// <summary>Gets or sets the start date.</summary>
    public DateTime StartDate { get; set; }
    /// <summary>Gets or sets the end date.</summary>
    public DateTime EndDate { get; set; }
    /// <summary>Gets or sets the duration in days.</summary>
    public decimal Days { get; set; }
    /// <summary>Gets or sets the reason.</summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>Gets or sets the current status (e.g., Pending, Approved).</summary>
    public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected, Cancelled
    /// <summary>Gets or sets the rejection reason, if applicable.</summary>
    public string? RejectionReason { get; set; }
    /// <summary>Gets or sets the request timestamp.</summary>
    public DateTime RequestedAt { get; set; }
    /// <summary>Gets or sets the collection of approval history records.</summary>
    public List<LeaveApprovalHistoryDto> ApprovalHistory { get; set; } = [];
}

/// <summary>
/// History of approval actions on a leave request.
/// </summary>
public sealed record LeaveApprovalHistoryDto
{
    /// <summary>Gets or sets the action performed.</summary>
    public string Action { get; set; } = string.Empty; // Submitted, Approved, Rejected
    /// <summary>Gets or sets the name of the person who performed the action.</summary>
    public string ActorName { get; set; } = string.Empty;
    /// <summary>Gets or sets the timestamp of the action.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Gets or sets optional comments.</summary>
    public string? Comments { get; set; }
}

/// <summary>
/// Request to approve or reject a leave request.
/// </summary>
public sealed record ApproveRejectLeaveRequest
{
    /// <summary>Gets or sets the decision (e.g., Approve, Reject).</summary>
    [Required]
    public string Decision { get; set; } = string.Empty; // Approve, Reject

    /// <summary>Gets or sets optional comments.</summary>
    public string? Comments { get; set; }
}
