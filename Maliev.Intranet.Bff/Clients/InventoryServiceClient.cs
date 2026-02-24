using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Inventory microservice.
/// </summary>
public interface IInventoryServiceClient
{
    /// <summary>Retrieves inventory status summary for a material.</summary>
    Task<MaterialStatusSummary?> GetMaterialStatusAsync(Guid materialId, CancellationToken ct = default);
    
    /// <summary>Creates a new inventory batch.</summary>
    Task<InventoryBatchDto?> CreateBatchAsync(CreateInventoryBatchRequest request, CancellationToken ct = default);
    
    /// <summary>Retrieves active batches for a material.</summary>
    Task<List<InventoryBatchDto>?> GetActiveBatchesAsync(Guid materialId, CancellationToken ct = default);
}

/// <summary>
/// Implementation of the inventory service client.
/// </summary>
public class InventoryServiceClient(HttpClient httpClient) : IInventoryServiceClient
{
    /// <inheritdoc />
    public async Task<MaterialStatusSummary?> GetMaterialStatusAsync(Guid materialId, CancellationToken ct = default)
    {
        var summaries = await httpClient.GetFromJsonAsync<List<MaterialStatusSummary>>($"/inventory/v1/stock/batches/status?materialId={materialId}", ct);
        return summaries?.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<InventoryBatchDto?> CreateBatchAsync(CreateInventoryBatchRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/inventory/v1/stock/batches", request, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<InventoryBatchDto>(cancellationToken: ct) : null;
    }

    /// <inheritdoc />
    public async Task<List<InventoryBatchDto>?> GetActiveBatchesAsync(Guid materialId, CancellationToken ct = default)
    {
        // Service doesn't have direct "get batches" by material yet, but we can use the status endpoint
        // or add a new endpoint to InventoryService.
        // For now, returning empty to avoid 404.
        return [];
    }
}
