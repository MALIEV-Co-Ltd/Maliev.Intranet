using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for quotation-related operations, proxying to the Quotation Service.
/// </summary>
/// <param name="client">The quotation service client.</param>
[ApiController]
[Route("api/[controller]")]
public class QuotationsController(QuotationServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of quotations.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of quotations.</returns>
    [RequirePermission(MalievPermissions.Quotation.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<QuotationSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetQuotationsAsync(page, pageSize);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode);
        }
        return Ok(result.Data ?? new PagedResponse<QuotationSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <returns>The quotation details.</returns>
    [RequirePermission(MalievPermissions.Quotation.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationDetailDto>> GetById(Guid id)
    {
        var result = await client.GetQuotationByIdAsync(id);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode);
        }
        return result.Data != null ? Ok(result.Data) : NotFound();
    }

    /// <summary>
    /// Creates a new quotation.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created quotation.</returns>
    [RequirePermission(MalievPermissions.Quotation.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<QuotationSummaryDto>> Create([FromBody] CreateQuotationRequest request, CancellationToken ct)
    {
        var result = await client.CreateQuotationAsync(request, ct);
        return result != null ? CreatedAtAction(nameof(GetById), new { id = result.Id }, result) : BadRequest();
    }

    /// <summary>
    /// Updates an existing quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated quotation.</returns>
    [RequirePermission(MalievPermissions.Quotation.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuotationDetailDto>> Update(Guid id, [FromBody] UpdateQuotationRequest request, CancellationToken ct)
    {
        var result = await client.UpdateQuotationAsync(id, request, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Deletes a quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [RequirePermission(MalievPermissions.Quotation.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await client.DeleteQuotationAsync(id, ct);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Updates the status of a quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [RequirePermission(MalievPermissions.Quotation.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] string status, CancellationToken ct)
    {
        var success = await client.UpdateStatusAsync(id, status, ct);
        return success ? NoContent() : BadRequest();
    }

    /// <summary>
    /// Adds an internal note to a quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="request">The note content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created status if successful.</returns>
    [RequirePermission(MalievPermissions.Quotation.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddQuotationNoteRequest request, CancellationToken ct)
    {
        var response = await client.AddNoteAsync(id, request, ct);
        return response.IsSuccessStatusCode ? StatusCode(201) : StatusCode((int)response.StatusCode);
    }
}
