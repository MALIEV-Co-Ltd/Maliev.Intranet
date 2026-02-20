using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for invoice-related operations, proxying to the Invoice Service.
/// </summary>
/// <param name="client">The invoice service client.</param>
[ApiController]
[Route("api/[controller]")]
public class InvoicesController(InvoiceServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of invoices.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paged list of invoices.</returns>
    [RequirePermission(MalievPermissions.Invoice.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<InvoiceSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await client.GetInvoicesAsync(page, pageSize, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<InvoiceSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single invoice.
    /// </summary>
    /// <param name="id">The invoice ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The invoice details.</returns>
    [RequirePermission(MalievPermissions.Invoice.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await client.GetInvoiceByIdAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new invoice.
    /// </summary>
    [RequirePermission(MalievPermissions.Invoice.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<InvoiceSummaryDto>> Create([FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        var result = await client.CreateInvoiceAsync(request, ct);
        return result != null ? CreatedAtAction(nameof(GetById), new { id = result.Id }, result) : BadRequest();
    }

    /// <summary>
    /// Updates an existing invoice.
    /// </summary>
    [RequirePermission(MalievPermissions.Invoice.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceDetailDto>> Update(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken ct)
    {
        var result = await client.UpdateInvoiceAsync(id, request, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Finalizes an invoice.
    /// </summary>
    [RequirePermission(MalievPermissions.Invoice.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/finalize")]
    public async Task<ActionResult> Finalize(Guid id, CancellationToken ct)
    {
        var success = await client.FinalizeInvoiceAsync(id, ct);
        return success ? Ok() : BadRequest();
    }

    /// <summary>
    /// Cancels an invoice.
    /// </summary>
    [RequirePermission(MalievPermissions.Invoice.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelInvoiceRequest request, CancellationToken ct)
    {
        var success = await client.CancelInvoiceAsync(id, request, ct);
        return success ? Ok() : BadRequest();
    }

    /// <summary>
    /// Splits an invoice into multiple child invoices.
    /// </summary>
    [RequirePermission(MalievPermissions.Invoice.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/split")]
    public async Task<ActionResult<List<InvoiceSummaryDto>>> SplitInvoice(Guid id, [FromBody] SplitInvoiceRequest request, CancellationToken ct)
    {
        var result = await client.SplitInvoiceAsync(id, request, ct);
        return Ok(result);
    }
}
