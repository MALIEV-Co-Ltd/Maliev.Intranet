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
    /// Creates one QR-tracked physical stock item for a received material.
    /// </summary>
    /// <param name="request">The item creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created inventory item.</returns>
    [RequirePermission(MalievPermissions.Inventory.StockWrite, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("items")]
    public async Task<ActionResult<InventoryItemDto>> CreateItem(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken ct)
    {
        var result = await client.CreateItemAsync(request, ct);
        return result is null ? StatusCode(StatusCodes.Status502BadGateway) : Ok(result);
    }

    /// <summary>
    /// Returns QR-tracked physical inventory items.
    /// </summary>
    /// <param name="materialId">Optional material filter.</param>
    /// <param name="status">Optional item status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching physical inventory items.</returns>
    [RequirePermission(MalievPermissions.Inventory.StockRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("items")]
    public async Task<ActionResult<List<InventoryItemDto>>> GetItems(
        [FromQuery] Guid? materialId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await client.GetItemsAsync(materialId, status, ct);
        return Ok(result);
    }

    /// <summary>
    /// Finds one physical inventory item by tracking code.
    /// </summary>
    /// <param name="trackingCode">The printed tracking code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching physical inventory item.</returns>
    [RequirePermission(MalievPermissions.Inventory.StockRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("items/{trackingCode}")]
    public async Task<ActionResult<InventoryItemDto>> GetItem(string trackingCode, CancellationToken ct)
    {
        var result = await client.GetItemAsync(trackingCode, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Finds one physical inventory item by tracking code or scanned QR payload.
    /// </summary>
    /// <param name="code">The printed tracking code or full scanned QR payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching physical inventory item.</returns>
    [RequirePermission(MalievPermissions.Inventory.StockRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("items/lookup")]
    public async Task<ActionResult<InventoryItemDto>> LookupItem([FromQuery] string code, CancellationToken ct)
    {
        var result = await client.GetItemAsync(code, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Consumes material from one exact QR-tracked physical inventory item.
    /// </summary>
    /// <param name="trackingCode">The printed tracking code.</param>
    /// <param name="request">The consumption request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated inventory item.</returns>
    [RequirePermission(MalievPermissions.Inventory.StockWrite, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("items/{trackingCode}/consume")]
    public async Task<ActionResult<InventoryItemDto>> ConsumeItem(
        string trackingCode,
        [FromBody] ConsumeInventoryItemRequest request,
        CancellationToken ct)
    {
        var result = await client.ConsumeItemAsync(trackingCode, request, ct);
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
