using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for basic leave-related operations and contextual information.
/// </summary>
public class LeaveDto
{
}

/// <summary>
/// Request model for submitting a new leave application.
/// </summary>
public sealed record SubmitLeaveRequestDto
{
    /// <summary>The category of leave being requested (e.g., Annual, Sick).</summary>
    [Required]
    public string LeaveType { get; set; } = string.Empty;

    /// <summary>The inclusive start date of the requested leave period.</summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>The inclusive end date of the requested leave period.</summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>Specific period for half-day leave requests (e.g., "Morning", "Afternoon").</summary>
    public string? HalfDayPeriod { get; set; }

    /// <summary>A detailed justification or reason for the leave request.</summary>
    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response model for a detailed view of an individual leave request and its history.
/// </summary>
public sealed record LeaveRequestDetailDto
{
    /// <summary>The unique identifier for the leave request.</summary>
    public Guid Id { get; set; }
    /// <summary>The identifier of the employee who submitted the request.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>The full display name of the employee who submitted the request.</summary>
    public string EmployeeName { get; set; } = string.Empty;
    /// <summary>The category of leave requested.</summary>
    public string LeaveType { get; set; } = string.Empty;
    /// <summary>The inclusive start date of the leave period.</summary>
    public DateTime StartDate { get; set; }
    /// <summary>The inclusive end date of the leave period.</summary>
    public DateTime EndDate { get; set; }
    /// <summary>The total number of working days calculated for the request.</summary>
    public decimal Days { get; set; }
    /// <summary>The justification provided by the employee.</summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>The current status of the request (e.g., Pending, Approved, Rejected, Cancelled).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The reason provided by the approver if the request was rejected.</summary>
    public string? RejectionReason { get; set; }
    /// <summary>The date and time when the request was first submitted.</summary>
    public DateTime RequestedAt { get; set; }
    /// <summary>Historical log of all approval actions and comments for this request.</summary>
    public List<LeaveApprovalHistoryDto> ApprovalHistory { get; set; } = [];
}

/// <summary>
/// Represents a single action or decision in the approval workflow of a leave request.
/// </summary>
public sealed record LeaveApprovalHistoryDto
{
    /// <summary>The action performed (e.g., Submitted, Approved, Rejected).</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>The display name of the user who performed the action.</summary>
    public string ActorName { get; set; } = string.Empty;
    /// <summary>The date and time when the action was recorded.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Optional comments or feedback provided during the action.</summary>
    public string? Comments { get; set; }
}

/// <summary>
/// Request model for an approver to submit a decision on a leave request.
/// </summary>
public sealed record ApproveRejectLeaveRequest
{
    /// <summary>The approval decision to apply (e.g., "Approve", "Reject").</summary>
    [Required]
    public string Decision { get; set; } = string.Empty;

    /// <summary>Optional comments or justification for the decision.</summary>
    public string? Comments { get; set; }
}
