using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Bff.Clients;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for order-related operations, proxying to the Order Service.
/// </summary>
/// <param name="client">The order service client.</param>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController(OrderServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of orders.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <returns>A paged list of orders.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderSummaryDto>>> Get([FromQuery] int page = 1)
    {
        var result = await client.GetOrdersAsync(page);
        return result != null ? Ok(result) : Ok(new PagedResponse<OrderSummaryDto>());
    }
}