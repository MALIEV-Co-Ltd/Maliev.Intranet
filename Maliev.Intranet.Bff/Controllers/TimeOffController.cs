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
public class TimeOffController(ILeaveServiceClient client) : ControllerBase
{
    /// <summary>
    /// Gets leave balances for the current user.
    /// </summary>
    [RequirePermission(MalievPermissions.Leave.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("balances")]
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetBalances(CancellationToken ct)
    {
        // TODO: Get real employee ID from user context. For MVP, we might need a way to map User ID -> Employee ID.
        // Assuming the JWT sub claim is the Employee ID for now, or we fetch it.
        // For development, we'll use a hardcoded test ID if not found, or throw.
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdString, out var employeeId))
        {
            // Fallback for dev/testing if sub isn't a guid
            employeeId = Guid.Empty;
        }

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
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdString, out var employeeId)) employeeId = Guid.Empty;
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
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdString, out var employeeId)) employeeId = Guid.Empty;

        var result = await client.SubmitRequestAsync(employeeId, request, ct);
        return result != null ? Ok(result) : BadRequest();
    }
}
