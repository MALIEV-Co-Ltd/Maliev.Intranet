using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for delivery note operations, proxying to the Delivery Service.
/// </summary>
/// <param name="client">The delivery service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DeliveryNotesController(IDeliveryServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of delivery notes.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<DeliveryNoteSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await client.GetDeliveryNotesAsync(page, pageSize, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<DeliveryNoteSummaryDto>());
    }

    /// <summary>
    /// Retrieves a delivery note by ID.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeliveryNoteDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await client.GetDeliveryNoteAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.Create, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<DeliveryNoteDetailDto>> Create([FromBody] CreateDeliveryNoteRequest request, CancellationToken ct)
    {
        var result = await client.CreateDeliveryNoteAsync(request, ct);
        return result != null ? CreatedAtAction(nameof(GetById), new { id = result.Id }, result) : BadRequest();
    }

    /// <summary>
    /// Updates the status of a delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.UpdateStatus, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<DeliveryNoteDetailDto>> UpdateStatus(Guid id, [FromBody] UpdateDeliveryStatusRequest request, CancellationToken ct)
    {
        var result = await client.UpdateDeliveryStatusAsync(id, request, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Generates a PDF for a delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.GeneratePdf, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/pdf")]
    public async Task<ActionResult<string>> GeneratePdf(Guid id, CancellationToken ct)
    {
        var url = await client.GeneratePdfAsync(id, ct);
        return url != null ? Ok(url) : BadRequest();
    }

    /// <summary>
    /// Deletes a delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.Delete, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await client.DeleteDeliveryNoteAsync(id, ct);
        return success ? NoContent() : NotFound();
    }
}
