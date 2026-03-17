using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// BFF controller that proxies all project lifecycle operations to the downstream ProjectService.
/// </summary>
/// <param name="client">The typed ProjectService HTTP client.</param>
[RequirePermission(MalievPermissions.Project.Read, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class ProjectsController(ProjectServiceClient client) : ControllerBase
{
    // ── Query endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paged list of projects with optional status/search filtering.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="search">Optional full-text search term.</param>
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged project summaries.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProjectSummaryDto>>> Get(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await client.GetProjectsAsync(status, search, customerId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns full detail for a single project.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Project detail or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await client.GetProjectByIdAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    // ── Create / Update / Delete ─────────────────────────────────────────────

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="request">Project creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created project detail.</returns>
    [HttpPost]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<ProjectDetailDto>> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var result = await client.CreateProjectAsync(request, ct);
        return result != null
            ? CreatedAtAction(nameof(GetById), new { id = result.Id }, result)
            : StatusCode(502, "ProjectService returned an error.");
    }

    /// <summary>
    /// Updates a project's metadata fields (title, description, notes, validity period).
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("{id:guid}")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> Update(Guid id, [FromBody] object request, CancellationToken ct)
    {
        var response = await client.UpdateProjectAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Deletes a project. Only permitted when the project is in Draft status.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("{id:guid}")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var response = await client.DeleteProjectAsync(id, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    // ── Parts ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new part (uploaded 3D file) to an existing project.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="request">Part creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created project part details.</returns>
    [HttpPost("{id:guid}/parts")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<ProjectPartDto>> AddPart(Guid id, [FromBody] AddProjectPartRequest request, CancellationToken ct)
    {
        var result = await client.AddPartAsync(id, request, ct);
        return result != null ? Ok(result) : StatusCode(502, "ProjectService returned an error.");
    }

    /// <summary>
    /// Updates an existing part's configuration (process, material, quantity, finish, etc.).
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The part GUID.</param>
    /// <param name="request">Configuration update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("{id:guid}/parts/{partId:guid}")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> UpdatePart(Guid id, Guid partId, [FromBody] UpdateProjectPartRequest request, CancellationToken ct)
    {
        var response = await client.UpdatePartAsync(id, partId, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Removes a part from a project.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The part GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("{id:guid}/parts/{partId:guid}")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> DeletePart(Guid id, Guid partId, CancellationToken ct)
    {
        var response = await client.DeletePartAsync(id, partId, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    // ── Pricing ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Triggers AI price estimation for a specific part and returns the full price breakdown.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The part GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Price breakdown DTO.</returns>
    [HttpPost("{id:guid}/parts/{partId:guid}/price")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<ProjectPriceBreakdownDto>> GetPartPrice(Guid id, Guid partId, CancellationToken ct)
    {
        var result = await client.GetPartPriceAsync(id, partId, ct);
        return result != null ? Ok(result) : StatusCode(502);
    }

    /// <summary>
    /// Confirms or overrides the AI-estimated price for a specific part.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The part GUID.</param>
    /// <param name="request">Confirmed price and optional override reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("{id:guid}/parts/{partId:guid}/confirm-price")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> ConfirmPartPrice(Guid id, Guid partId, [FromBody] ConfirmPartPriceRequest request, CancellationToken ct)
    {
        var response = await client.ConfirmPartPriceAsync(id, partId, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    // ── Quotation lifecycle ──────────────────────────────────────────────────

    /// <summary>
    /// Generates the quotation PDF and marks the project as Quoted.
    /// Only allowed when all parts have confirmed prices.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("{id:guid}/generate-quotation")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> GenerateQuotation(Guid id, CancellationToken ct)
    {
        var response = await client.GenerateQuotationAsync(id, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Marks the project quotation as accepted by the customer, triggering order/job creation.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("{id:guid}/accept-quotation")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> AcceptQuotation(Guid id, CancellationToken ct)
    {
        var response = await client.AcceptQuotationAsync(id, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }
}
