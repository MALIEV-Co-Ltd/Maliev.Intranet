using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for leave-related operations, proxying to the Employee Service.
/// </summary>
/// <param name="client">The time-off service client.</param>
[RequirePermission(MalievPermissions.Leave.Read, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class TimeOffController(TimeOffServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves leave balances for the current user.
    /// </summary>
    /// <returns>A list of leave balances.</returns>
    [HttpGet("balances")]
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetBalances()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdString, out var employeeId))
        {
            return BadRequest("Invalid user identifier.");
        }

        var result = await client.GetLeaveBalancesAsync(employeeId);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves leave requests for the current user.
    /// </summary>
    /// <returns>A list of leave requests.</returns>
    [HttpGet("requests")]
    public async Task<ActionResult<List<LeaveRequestSummaryDto>>> GetRequests()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdString, out var employeeId))
        {
            return BadRequest("Invalid user identifier.");
        }

        var result = await client.GetLeaveRequestsAsync(employeeId);
        return Ok(result);
    }
}
