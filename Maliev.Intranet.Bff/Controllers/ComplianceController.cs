using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for compliance-related operations, proxying to the Compliance Service.
/// </summary>
/// <param name="client">The compliance service client.</param>
[ApiController]
[Route("api/[controller]")]
public class ComplianceController(IComplianceServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves compliance statistics.
    /// </summary>
    [RequirePermission(MalievPermissions.Compliance.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("stats")]
    public async Task<ActionResult<ComplianceStatsDto>> GetStats()
    {
        var result = await client.GetComplianceStatsAsync();
        return result != null ? Ok(result) : Ok(new ComplianceStatsDto());
    }

    /// <summary>
    /// Retrieves a paged list of compliance records.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of compliance records.</returns>
    [RequirePermission(MalievPermissions.Compliance.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ComplianceRecordDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetComplianceRecordsAsync(page, pageSize);
        return result != null ? Ok(result) : Ok(new PagedResponse<ComplianceRecordDto>());
    }

    /// <summary>
    /// Retrieves a compliance record by ID.
    /// </summary>
    /// <param name="id">The record ID.</param>
    /// <returns>The compliance record.</returns>
    [RequirePermission(MalievPermissions.Compliance.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ComplianceRecordDto>> GetById(Guid id)
    {
        var result = await client.GetComplianceRecordByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new compliance record.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created compliance record.</returns>
    [RequirePermission(MalievPermissions.Compliance.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<ComplianceRecordDto>> Create([FromBody] CreateComplianceRecordRequest request, CancellationToken ct)
    {
        var result = await client.CreateComplianceRecordAsync(request, ct);
        return result != null ? CreatedAtAction(nameof(GetById), new { id = result.Id }, result) : BadRequest();
    }

    /// <summary>
    /// Deletes a compliance record.
    /// </summary>
    /// <param name="id">The record ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [RequirePermission(MalievPermissions.Compliance.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await client.DeleteComplianceRecordAsync(id, ct);
        return success ? NoContent() : NotFound();
    }
}
