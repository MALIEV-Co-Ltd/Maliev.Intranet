using Asp.Versioning;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for time-off operations, proxying to the Leave Service.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TimeOffController(ILeaveServiceClient client, EmployeeServiceClient employeeServiceClient) : ControllerBase
{
    private async Task<EmployeeDetailDto?> GetEmployeeProfileAsync(CancellationToken ct)
    {
        // Try to get explicit employee_id claim first (if enriched)
        var employeeIdClaim = User.FindFirst("employee_id")?.Value;
        if (Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return await employeeServiceClient.GetEmployeeByIdAsync(employeeId, ct)
                ?? new EmployeeDetailDto { Id = employeeId };
        }

        // Fallback: look up by Principal ID (sub)
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdString, out var principalId))
        {
            var employee = await employeeServiceClient.GetByPrincipalIdAsync(principalId, ct);
            if (employee != null)
            {
                return await employeeServiceClient.GetEmployeeByIdAsync(employee.Id, ct) ?? employee;
            }
        }

        return null;
    }

    private async Task<Guid> GetEmployeeIdAsync(CancellationToken ct)
    {
        var employee = await GetEmployeeProfileAsync(ct);
        return employee?.Id ?? Guid.Empty;
    }

    /// <summary>
    /// Gets leave balances for the current user.
    /// </summary>
    [RequirePermission(MalievPermissions.Leave.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("balances")]
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetBalances(CancellationToken ct)
    {
        var employeeId = await GetEmployeeIdAsync(ct);
        if (employeeId == Guid.Empty) return Unauthorized();

        var result = await client.GetMyBalancesAsync(employeeId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets leave requests for the current user.
    /// </summary>
    [RequirePermission(MalievPermissions.Leave.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("requests")]
    public async Task<ActionResult<List<LeaveRequestSummaryDto>>> GetRequests(CancellationToken ct)
    {
        var employeeId = await GetEmployeeIdAsync(ct);
        if (employeeId == Guid.Empty) return Unauthorized();

        var result = await client.GetMyRequestsAsync(employeeId, DateTime.UtcNow.Year, ct);
        return Ok(result);
    }

    /// <summary>
    /// Submits a new leave request.
    /// </summary>
    [RequirePermission(MalievPermissions.Leave.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("requests")]
    public async Task<ActionResult<LeaveRequestDetailDto>> SubmitRequest([FromBody] SubmitLeaveRequestDto request, CancellationToken ct)
    {
        var employee = await GetEmployeeProfileAsync(ct);
        if (employee is null || employee.Id == Guid.Empty) return Unauthorized();

        var approverId = Guid.TryParse(employee.ManagerId, out var managerId) && managerId != employee.Id
            ? managerId
            : (Guid?)null;

        var result = await client.SubmitRequestAsync(employee.Id, request, approverId, ct);
        return result != null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Gets pending leave approvals assigned to the current user.
    /// </summary>
    [RequirePermission(MalievPermissions.Leave.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("approvals")]
    public async Task<ActionResult<List<LeaveRequestDetailDto>>> GetApprovals(CancellationToken ct)
    {
        var employeeId = await GetEmployeeIdAsync(ct);
        if (employeeId == Guid.Empty) return Unauthorized();

        var result = await client.GetPendingApprovalsAsync(employeeId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Approves or rejects a pending leave request assigned to the current user.
    /// </summary>
    [RequirePermission(MalievPermissions.Leave.Approve, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("requests/{requestId:guid}/decision")]
    public async Task<IActionResult> ProcessDecision(
        Guid requestId,
        [FromBody] ApproveRejectLeaveRequest request,
        CancellationToken ct)
    {
        var employeeId = await GetEmployeeIdAsync(ct);
        if (employeeId == Guid.Empty) return Unauthorized();

        var success = await client.ProcessDecisionAsync(requestId, employeeId, request, ct);
        return success ? Ok() : BadRequest();
    }
}
