using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for invoice-related operations, proxying to the Invoice Service.
/// </summary>
/// <param name="client">The invoice service client.</param>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InvoicesController(InvoiceServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of invoices.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of invoices.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<InvoiceSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetInvoicesAsync(page, pageSize);
        return result != null ? Ok(result) : Ok(new PagedResponse<InvoiceSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single invoice.
    /// </summary>
    /// <param name="id">The invoice ID.</param>
    /// <returns>The invoice details.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDetailDto>> GetById(Guid id)
    {
        var result = await client.GetInvoiceByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }
}
