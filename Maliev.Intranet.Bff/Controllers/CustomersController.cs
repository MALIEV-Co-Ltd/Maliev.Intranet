using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Bff.Clients;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for customer-related operations, proxying to the Customer Service.
/// </summary>
/// <param name="client">The customer service client.</param>
[Authorize(Policy = MalievPermissions.Customer.Read)]
[ApiController]
[Route("api/[controller]")]
public class CustomersController(CustomerServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of customers.
    /// </summary>
    /// <param name="query">Optional search query.</param>
    /// <param name="page">The page number.</param>
    /// <returns>A paged list of customers.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerSummaryDto>>> Get([FromQuery] string? query, [FromQuery] int page = 1)
    {
        var result = await client.GetCustomersAsync(query, page);
        return result != null ? Ok(result) : Ok(new PagedResponse<CustomerSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single customer.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <returns>The customer details.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(Guid id)
    {
        var result = await client.GetCustomerByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }
}
