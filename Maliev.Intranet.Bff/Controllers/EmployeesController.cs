using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for employee-related operations, proxying to the Employee Service.
/// </summary>
/// <param name="client">The employee service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EmployeesController(EmployeeServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of employees.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paged list of employees.</returns>
    [RequirePermission(MalievPermissions.Employee.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<EmployeeSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await client.GetEmployeesAsync(page, pageSize, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<EmployeeSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single employee.
    /// </summary>
    /// <param name="id">The employee ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The employee details.</returns>
    [RequirePermission(MalievPermissions.Employee.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await client.GetEmployeeByIdAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new employee.
    /// </summary>
    [RequirePermission(MalievPermissions.Employee.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<EmployeeDetailDto>> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var result = await client.CreateEmployeeAsync(request, ct);
        return result != null ? CreatedAtAction(nameof(GetById), new { id = result.Id }, result) : BadRequest();
    }

    /// <summary>
    /// Updates an existing employee.
    /// </summary>
    [RequirePermission(MalievPermissions.Employee.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeDetailDto>> Update(Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        var result = await client.UpdateEmployeeAsync(id, request, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Terminates an employee.
    /// </summary>
    [RequirePermission(MalievPermissions.Employee.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/terminate")]
    public async Task<ActionResult> Terminate(Guid id, [FromBody] TerminateEmployeeRequest request, CancellationToken ct)
    {
        var success = await client.TerminateEmployeeAsync(id, request, ct);
        return success ? Ok() : BadRequest();
    }

    /// <summary>
    /// Retrieves the current user's employee profile.
    /// </summary>
    [RequirePermission(MalievPermissions.Employee.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("me")]
    public async Task<ActionResult<EmployeeDetailDto>> GetMe(CancellationToken ct)
    {
        var principalIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        if (!Guid.TryParse(principalIdStr, out var principalId))
            return NotFound();

        var result = await client.GetByPrincipalIdAsync(principalId, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Retrieves the current user's editable self-service profile fields.
    /// </summary>
    [RequirePermission(MalievPermissions.Employee.ProfileRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("me/profile")]
    public async Task<ActionResult<EmployeeSelfProfileDto>> GetMyProfile(CancellationToken ct)
    {
        var employee = await ResolveCurrentEmployeeAsync(ct);
        if (employee is null)
        {
            return NotFound();
        }

        var profile = await client.GetSelfServiceProfileAsync(employee.Id, ct);
        return profile != null ? Ok(profile) : NotFound();
    }

    /// <summary>
    /// Updates the current user's allowed self-service profile fields.
    /// </summary>
    [RequirePermission(MalievPermissions.Employee.ProfileUpdate, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("me/profile")]
    public async Task<ActionResult<EmployeeSelfProfileDto>> UpdateMyProfile([FromBody] UpdateEmployeeSelfProfileRequest request, CancellationToken ct)
    {
        var employee = await ResolveCurrentEmployeeAsync(ct);
        if (employee is null)
        {
            return NotFound();
        }

        var updated = await client.UpdateSelfServiceProfileAsync(employee.Id, request, ct);
        if (!updated)
        {
            return NotFound();
        }

        var profile = await client.GetSelfServiceProfileAsync(employee.Id, ct);
        return profile != null ? Ok(profile) : Ok();
    }

    /// <summary>
    /// Retrieves HR analytics data.
    /// </summary>
    [RequirePermission(MalievPermissions.Employee.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("analytics")]
    public async Task<ActionResult<HrAnalyticsDto>> GetAnalytics(CancellationToken ct)
    {
        var result = await client.GetHrAnalyticsAsync(ct);
        return result != null ? Ok(result) : Ok(new HrAnalyticsDto());
    }

    /// <summary>
    /// Retrieves the organizational chart data.
    /// </summary>
    [RequirePermission(MalievPermissions.Employee.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("org-chart")]
    public async Task<ActionResult<List<OrgNodeDto>>> GetOrgChart(CancellationToken ct)
    {
        var result = await client.GetOrgChartAsync(ct);
        return result != null ? Ok(result) : Ok(new List<OrgNodeDto>());
    }

    /// <summary>
    /// Adds an internal HR note to an employee.
    /// </summary>
    [RequirePermission(MalievPermissions.Employee.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddEmployeeNoteRequest request, CancellationToken ct)
    {
        var response = await client.AddNoteAsync(id, request, ct);
        return response.IsSuccessStatusCode ? StatusCode(201) : StatusCode((int)response.StatusCode);
    }

    private async Task<EmployeeDetailDto?> ResolveCurrentEmployeeAsync(CancellationToken ct)
    {
        var principalIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(principalIdStr, out var principalId)
            ? await client.GetByPrincipalIdAsync(principalId, ct)
            : null;
    }
}
