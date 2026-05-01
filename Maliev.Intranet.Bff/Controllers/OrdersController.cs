using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for order-related operations, proxying to the Order Service.
/// </summary>
/// <param name="client">The order service client.</param>
[RequirePermission(MalievPermissions.Order.Read, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrdersController(OrderServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of orders.
    /// </summary>
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="page">The page number.</param>
    /// <param name="search">Optional free-text search term matched against order number and customer name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paged list of orders.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderSummaryDto>>> Get([FromQuery] Guid? customerId = null, [FromQuery] int page = 1, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await client.GetOrdersAsync(customerId, page, search, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<OrderSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single order.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The order details.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDetailDto>> GetById(string id, CancellationToken ct)
    {
        var result = await client.GetOrderByIdAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [RequirePermission(MalievPermissions.Order.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateOrderRequest request, CancellationToken ct)
    {
        var response = await client.UpdateOrderAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Updates the status of an order (e.g., from Kanban drag-and-drop).
    /// </summary>
    [RequirePermission(MalievPermissions.Order.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var response = await client.UpdateStatusAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }
}
