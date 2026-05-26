using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for inventory batch operations, proxying to InventoryService.
/// </summary>
/// <param name="client">The typed InventoryService client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class InventoryController(InventoryServiceClient client) : ControllerBase
{
    /// <summary>
    /// Creates a traceable inventory batch for a received material.
    /// </summary>
    /// <param name="request">The batch creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created batch.</returns>
    [RequirePermission(MalievPermissions.Inventory.BatchesWrite, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("batches")]
    public async Task<ActionResult<InventoryBatchDto>> CreateBatch(
        [FromBody] CreateInventoryBatchRequest request,
        CancellationToken ct)
    {
        var result = await client.CreateBatchAsync(request, ct);
        return result is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(result);
    }

    /// <summary>
    /// Returns inventory batch status summaries.
    /// </summary>
    /// <param name="materialId">Optional material filter.</param>
    /// <param name="status">Optional batch status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching status summaries.</returns>
    [RequirePermission(MalievPermissions.Inventory.BatchesRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("batches/status")]
    public async Task<ActionResult<List<MaterialInventoryStatusDto>>> GetBatchStatus(
        [FromQuery] Guid? materialId = null,
        [FromQuery] string? status = "Active",
        CancellationToken ct = default)
    {
        var result = await client.GetBatchStatusAsync(materialId, status, ct);
        return Ok(result);
    }
}
