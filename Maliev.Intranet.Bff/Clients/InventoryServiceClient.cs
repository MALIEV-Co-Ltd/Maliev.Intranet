using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with InventoryService stock and material batch endpoints.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class InventoryServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Creates a traceable physical stock batch for a material.
    /// </summary>
    /// <param name="request">The batch creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created inventory batch when successful.</returns>
    public async Task<InventoryBatchDto?> CreateBatchAsync(CreateInventoryBatchRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/inventory/v1/stock/batches", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<InventoryBatchDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Gets inventory status summaries for active material batches.
    /// </summary>
    /// <param name="materialId">Optional material identifier filter.</param>
    /// <param name="status">Optional batch lifecycle status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching material inventory statuses.</returns>
    public async Task<List<MaterialInventoryStatusDto>> GetBatchStatusAsync(
        Guid? materialId = null,
        string? status = "Active",
        CancellationToken ct = default)
    {
        var query = new List<string>();
        if (materialId.HasValue)
        {
            query.Add($"materialId={Uri.EscapeDataString(materialId.Value.ToString())}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status.Trim())}");
        }

        var path = query.Count == 0
            ? "/inventory/v1/stock/batches/status"
            : $"/inventory/v1/stock/batches/status?{string.Join("&", query)}";

        return await httpClient.GetFromJsonAsync<List<MaterialInventoryStatusDto>>(path, ct) ?? [];
    }
}
