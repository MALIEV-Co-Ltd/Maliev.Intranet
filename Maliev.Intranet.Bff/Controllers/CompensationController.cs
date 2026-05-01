using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for compensation-related operations, proxying to the Compensation Service.
/// </summary>
/// <param name="client">The compensation service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CompensationController(ICompensationServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves compensation summary.
    /// </summary>
    [RequirePermission(MalievPermissions.Compensation.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("summary")]
    public async Task<ActionResult<CompensationSummaryDto>> GetSummary(CancellationToken ct)
    {
        var result = await client.GetSummaryAsync(ct);
        return result != null ? Ok(result) : Ok(new CompensationSummaryDto());
    }

    /// <summary>
    /// Retrieves a list of benefits.
    /// </summary>
    [RequirePermission(MalievPermissions.Compensation.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("benefits")]
    public async Task<ActionResult<List<BenefitDto>>> GetBenefits(CancellationToken ct)
    {
        var result = await client.GetBenefitsAsync(ct);
        return result != null ? Ok(result) : Ok(new List<BenefitDto>());
    }
}
