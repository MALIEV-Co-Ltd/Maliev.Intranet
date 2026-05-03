using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for procurement-related operations, proxying to PurchaseOrderService.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProcurementController(IPurchaseOrderServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of purchase orders.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PurchaseOrderDto>>> Get(
        [FromQuery] string? status = null,
        [FromQuery] string? orderType = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] int? orderId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await client.GetPurchaseOrdersAsync(
            status,
            orderType,
            supplierId,
            orderId,
            fromDate,
            toDate,
            sortBy,
            sortDirection,
            page,
            pageSize,
            ct);

        return result is not null
            ? Ok(result)
            : StatusCode(StatusCodes.Status502BadGateway, "Purchase orders could not be loaded.");
    }

    /// <summary>
    /// Retrieves a purchase order by ID.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(int id, CancellationToken ct)
    {
        var result = await client.GetPurchaseOrderByIdAsync(id, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new purchase order.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var result = await client.CreatePurchaseOrderAsync(request, ct);
        return result is not null ? CreatedAtAction(nameof(GetById), new { id = result.Id }, result) : BadRequest();
    }

    /// <summary>
    /// Approves a purchase order.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<PurchaseOrderDto>> Approve(int id, CancellationToken ct)
    {
        var result = await client.ApprovePurchaseOrderAsync(id, ct);
        return result is not null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Sends a purchase order to the supplier.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:int}/send-to-supplier")]
    public async Task<ActionResult<PurchaseOrderDto>> SendToSupplier(int id, CancellationToken ct)
    {
        var result = await client.SendToSupplierAsync(id, ct);
        return result is not null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Marks purchase order goods as received.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:int}/receive")]
    public async Task<ActionResult<PurchaseOrderDto>> Receive(
        int id,
        [FromQuery] bool isPartialReceipt = false,
        CancellationToken ct = default)
    {
        var result = await client.ReceivePurchaseOrderAsync(id, isPartialReceipt, ct);
        return result is not null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Cancels a purchase order.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelPurchaseOrderRequest request, CancellationToken ct)
    {
        var result = await client.CancelPurchaseOrderAsync(id, request, ct);
        return result ? NoContent() : BadRequest();
    }

    /// <summary>
    /// Exports a purchase order in the requested format.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:int}/export")]
    public async Task<IActionResult> Export(int id, [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        using var response = await client.ExportPurchaseOrderAsync(id, format, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, body);
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        return Content(body, contentType);
    }
}
