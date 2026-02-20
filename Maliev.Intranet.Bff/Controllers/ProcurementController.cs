using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for procurement-related operations, proxying to the Purchase Order Service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProcurementController(IPurchaseOrderServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of purchase orders.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PurchaseOrderDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await client.GetPurchaseOrdersAsync(page, pageSize, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<PurchaseOrderDto>());
    }

    /// <summary>
    /// Retrieves a purchase order by ID.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await client.GetPurchaseOrderByIdAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new purchase order.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var result = await client.CreatePurchaseOrderAsync(request, ct);
        return result != null ? CreatedAtAction(nameof(GetById), new { id = result.Id }, result) : BadRequest();
    }
}
