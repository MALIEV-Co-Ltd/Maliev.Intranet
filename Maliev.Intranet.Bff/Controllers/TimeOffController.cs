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
public class TimeOffController(ILeaveServiceClient client, EmployeeServiceClient employeeClient) : ControllerBase
{
    private async Task<Guid> GetEmployeeIdAsync(CancellationToken ct)
    {
        var principalIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(principalIdStr, out var principalId))
            return Guid.Empty;

        var profile = await employeeClient.GetByPrincipalIdAsync(principalId, ct);
        return profile?.Id ?? Guid.Empty;
    }

    /// <summary>
    /// Gets leave balances for the current user.
    /// </summary>
    [RequirePermission(MalievPermissions.Leave.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("balances")]
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetBalances(CancellationToken ct)
    {
        var employeeId = await GetEmployeeIdAsync(ct);
        if (employeeId == Guid.Empty) return Ok(new List<LeaveBalanceDto>());

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
        if (employeeId == Guid.Empty) return Ok(new List<LeaveRequestSummaryDto>());

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
        if (employeeId == Guid.Empty) return BadRequest(new { message = "Current user is not linked to an employee record." });

        var result = await client.SubmitRequestAsync(employeeId, request, ct);
        return result != null ? Ok(result) : BadRequest();
    }
}
