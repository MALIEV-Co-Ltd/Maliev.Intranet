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
[Route("api/[controller]")]
public class TimeOffController(ILeaveServiceClient client, EmployeeServiceClient employeeServiceClient) : ControllerBase
{
    private async Task<Guid> GetEmployeeIdAsync(CancellationToken ct)
    {
        // Try to get explicit employee_id claim first (if enriched)
        var employeeIdClaim = User.FindFirst("employee_id")?.Value;
        if (Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return employeeId;
        }

        // Fallback: look up by Principal ID (sub)
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdString, out var principalId))
        {
            var employee = await employeeServiceClient.GetByPrincipalIdAsync(principalId, ct);
            if (employee != null)
            {
                return employee.Id;
            }
        }

        return Guid.Empty;
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
        var employeeId = await GetEmployeeIdAsync(ct);
        if (employeeId == Guid.Empty) return Unauthorized();

        var result = await client.SubmitRequestAsync(employeeId, request, ct);
        return result != null ? Ok(result) : BadRequest();
    }
}
