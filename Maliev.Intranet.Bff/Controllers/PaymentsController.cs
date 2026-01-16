using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Bff.Clients;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for payment-related operations, proxying to the Payment Service.
/// </summary>
/// <param name="client">The payment service client.</param>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController(PaymentServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of payments.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of payments.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PaymentSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetPaymentsAsync(page, pageSize);
        return result != null ? Ok(result) : Ok(new PagedResponse<PaymentSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single payment.
    /// </summary>
    /// <param name="id">The payment ID.</param>
    /// <returns>The payment details.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDetailDto>> GetById(Guid id)
    {
        var result = await client.GetPaymentByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }
}
