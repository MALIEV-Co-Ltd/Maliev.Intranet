using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for supplier-related operations, proxying to the Supplier Service.
/// </summary>
/// <param name="client">The supplier service client.</param>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SuppliersController(SupplierServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of suppliers.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of suppliers.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<SupplierSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetSuppliersAsync(page, pageSize);
        return result != null ? Ok(result) : Ok(new PagedResponse<SupplierSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single supplier.
    /// </summary>
    /// <param name="id">The supplier ID.</param>
    /// <returns>The supplier details.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDetailDto>> GetById(Guid id)
    {
        var result = await client.GetSupplierByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }
}
