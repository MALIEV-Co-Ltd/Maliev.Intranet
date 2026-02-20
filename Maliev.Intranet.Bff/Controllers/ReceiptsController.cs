using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for receipt-related operations, proxying to the Receipt Service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReceiptsController(IReceiptServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of receipts.
    /// </summary>
    [RequirePermission(MalievPermissions.Receipt.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ReceiptDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await client.GetReceiptsAsync(page, pageSize, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<ReceiptDto>());
    }

    /// <summary>
    /// Retrieves a receipt by ID.
    /// </summary>
    [RequirePermission(MalievPermissions.Receipt.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReceiptDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await client.GetReceiptByIdAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new receipt.
    /// </summary>
    [RequirePermission(MalievPermissions.Receipt.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<ReceiptDto>> Create([FromBody] CreateReceiptRequest request, CancellationToken ct)
    {
        var result = await client.CreateReceiptAsync(request, ct);
        return result != null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Voids a receipt.
    /// </summary>
    [RequirePermission(MalievPermissions.Receipt.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult> Void(Guid id, CancellationToken ct)
    {
        var success = await client.VoidReceiptAsync(id, ct);
        return success ? NoContent() : BadRequest();
    }
}
