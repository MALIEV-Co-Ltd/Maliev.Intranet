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
    [Required]
    public string LeaveType { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string? HalfDayPeriod { get; set; } // Morning, Afternoon, or null

    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response for a leave request detail view.
/// </summary>
public sealed record LeaveRequestDetailDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Days { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected, Cancelled
    public string? RejectionReason { get; set; }
    public DateTime RequestedAt { get; set; }
    public List<LeaveApprovalHistoryDto> ApprovalHistory { get; set; } = [];
}

/// <summary>
/// History of approval actions on a leave request.
/// </summary>
public sealed record LeaveApprovalHistoryDto
{
    public string Action { get; set; } = string.Empty; // Submitted, Approved, Rejected
    public string ActorName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Comments { get; set; }
}

/// <summary>
/// Request to approve or reject a leave request.
/// </summary>
public sealed record ApproveRejectLeaveRequest
{
    [Required]
    public string Decision { get; set; } = string.Empty; // Approve, Reject

    public string? Comments { get; set; }
}
