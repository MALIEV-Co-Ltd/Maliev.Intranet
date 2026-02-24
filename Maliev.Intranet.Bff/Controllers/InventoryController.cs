using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Aspire.ServiceDefaults.Authorization;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// BFF Controller for inventory management.
/// </summary>
[ApiController]
[Route("api/inventory")]
[RequirePermission(MalievPermissions.Inventory.Read)]
public class InventoryController(IInventoryServiceClient inventoryClient) : ControllerBase
{
    /// <summary>Retrieves inventory status summary for a material.</summary>
    [HttpGet("materials/{materialId}/status")]
    public async Task<ActionResult<MaterialStatusSummary>> GetMaterialStatus(Guid materialId, CancellationToken ct)
    {
        var status = await inventoryClient.GetMaterialStatusAsync(materialId, ct);
        return status == null ? NotFound() : Ok(status);
    }

    /// <summary>Retrieves active batches for a material.</summary>
    [HttpGet("materials/{materialId}/batches/active")]
    public async Task<ActionResult<List<InventoryBatchDto>>> GetActiveBatches(Guid materialId, CancellationToken ct)
    {
        var batches = await inventoryClient.GetActiveBatchesAsync(materialId, ct);
        return batches == null ? StatusCode(502) : Ok(batches);
    }

    /// <summary>Creates a new inventory batch.</summary>
    [HttpPost("batches")]
    [RequirePermission(MalievPermissions.Inventory.Write)]
    public async Task<ActionResult<InventoryBatchDto>> CreateBatch([FromBody] CreateInventoryBatchRequest request, CancellationToken ct)
    {
        var batch = await inventoryClient.CreateBatchAsync(request, ct);
        return batch == null ? BadRequest() : CreatedAtAction(nameof(GetActiveBatches), new { materialId = batch.MaterialId }, batch);
    }
}
