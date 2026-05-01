using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for performance-related operations, proxying to the Performance Service.
/// </summary>
/// <param name="client">The performance service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PerformanceController(IPerformanceServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of performance reviews.
    /// </summary>
    [RequirePermission(MalievPermissions.Performance.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("reviews")]
    public async Task<ActionResult<List<PerformanceReviewDto>>> GetReviews(CancellationToken ct)
    {
        var result = await client.GetReviewsAsync(ct);
        return result != null ? Ok(result) : Ok(new List<PerformanceReviewDto>());
    }

    /// <summary>
    /// Retrieves a list of goals.
    /// </summary>
    [RequirePermission(MalievPermissions.Performance.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("goals")]
    public async Task<ActionResult<List<GoalDto>>> GetGoals(CancellationToken ct)
    {
        var result = await client.GetGoalsAsync(ct);
        return result != null ? Ok(result) : Ok(new List<GoalDto>());
    }
}
