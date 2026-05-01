using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for onboarding-related operations, proxying to the Lifecycle Service.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OnboardingController(ILifecycleServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of onboarding summaries.
    /// </summary>
    [RequirePermission(MalievPermissions.Onboarding.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<OnboardingSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await client.GetOnboardingsAsync(page, pageSize, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<OnboardingSummaryDto>());
    }

    /// <summary>
    /// Retrieves an onboarding checklist for an employee.
    /// </summary>
    [RequirePermission(MalievPermissions.Onboarding.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{employeeId:guid}/checklist")]
    public async Task<ActionResult<OnboardingChecklistDto>> GetChecklist(Guid employeeId, CancellationToken ct)
    {
        var result = await client.GetChecklistAsync(employeeId, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Updates the status of an onboarding task.
    /// </summary>
    [RequirePermission(MalievPermissions.Onboarding.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{employeeId:guid}/tasks")]
    public async Task<ActionResult> UpdateTask(Guid employeeId, [FromBody] UpdateOnboardingProgressRequest request, CancellationToken ct)
    {
        var success = await client.UpdateTaskAsync(employeeId, request, ct);
        return success ? NoContent() : BadRequest();
    }
}
