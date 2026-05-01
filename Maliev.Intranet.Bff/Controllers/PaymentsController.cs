using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for payment-related operations, proxying to the Payment Service.
/// </summary>
/// <param name="client">The payment service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PaymentsController(PaymentServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves payment metrics.
    /// </summary>
    /// <returns>Payment statistics.</returns>
    [RequirePermission(MalievPermissions.Payment.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("stats")]
    public async Task<ActionResult<PaymentStatsDto>> GetStats()
    {
        var result = await client.GetPaymentStatsAsync();
        return result != null ? Ok(result) : Ok(new PaymentStatsDto());
    }

    /// <summary>
    /// Retrieves a paged list of payments.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of payments.</returns>
    [RequirePermission(MalievPermissions.Payment.Read, AuthenticationSchemes = "Bearer,Cookies")]
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
    [RequirePermission(MalievPermissions.Payment.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDetailDto>> GetById(Guid id)
    {
        var result = await client.GetPaymentByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Records a new payment.
    /// </summary>
    [RequirePermission(MalievPermissions.Payment.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var response = await client.CreatePaymentAsync(request, ct);
        return response.IsSuccessStatusCode ? StatusCode(201) : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Allocates a payment to invoices.
    /// </summary>
    [RequirePermission(MalievPermissions.Payment.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/allocate")]
    public async Task<IActionResult> Allocate(Guid id, [FromBody] AllocatePaymentRequest request, CancellationToken ct)
    {
        var response = await client.AllocatePaymentAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Voids a payment.
    /// </summary>
    [RequirePermission(MalievPermissions.Payment.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/void")]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidPaymentRequest request, CancellationToken ct)
    {
        var response = await client.VoidPaymentAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }
}
