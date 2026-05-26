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
    /// Creates one QR-tracked physical stock item for a material.
    /// </summary>
    /// <param name="request">The item creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created inventory item when successful.</returns>
    public async Task<InventoryItemDto?> CreateItemAsync(CreateInventoryItemRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/inventory/v1/stock/items", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<InventoryItemDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Gets QR-tracked physical inventory items.
    /// </summary>
    /// <param name="materialId">Optional material identifier filter.</param>
    /// <param name="status">Optional item lifecycle status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching physical inventory items.</returns>
    public async Task<List<InventoryItemDto>> GetItemsAsync(
        Guid? materialId = null,
        string? status = null,
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
            ? "/inventory/v1/stock/items"
            : $"/inventory/v1/stock/items?{string.Join("&", query)}";

        return await httpClient.GetFromJsonAsync<List<InventoryItemDto>>(path, ct) ?? [];
    }

    /// <summary>
    /// Gets one QR-tracked physical inventory item by tracking code or QR payload.
    /// </summary>
    /// <param name="trackingCodeOrPayload">The tracking code or scanned QR payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching item when found.</returns>
    public async Task<InventoryItemDto?> GetItemAsync(string trackingCodeOrPayload, CancellationToken ct = default)
    {
        var trackingCode = NormalizeTrackingCode(trackingCodeOrPayload);
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var encoded = Uri.EscapeDataString(trackingCode);
        var response = await httpClient.GetAsync($"/inventory/v1/stock/items/{encoded}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<InventoryItemDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Consumes material from one exact QR-tracked inventory item.
    /// </summary>
    /// <param name="trackingCodeOrPayload">The tracking code or scanned QR payload.</param>
    /// <param name="request">The consumption request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated item when successful.</returns>
    public async Task<InventoryItemDto?> ConsumeItemAsync(
        string trackingCodeOrPayload,
        ConsumeInventoryItemRequest request,
        CancellationToken ct = default)
    {
        var trackingCode = NormalizeTrackingCode(trackingCodeOrPayload);
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var encoded = Uri.EscapeDataString(trackingCode);
        var response = await httpClient.PostAsJsonAsync($"/inventory/v1/stock/items/{encoded}/consume", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<InventoryItemDto>(cancellationToken: ct);
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

    private static string NormalizeTrackingCode(string value)
    {
        var trimmed = value.Trim();
        var slashIndex = trimmed.LastIndexOf("/", StringComparison.Ordinal);
        if (slashIndex >= 0 && slashIndex < trimmed.Length - 1)
        {
            trimmed = trimmed[(slashIndex + 1)..];
        }

        return trimmed.ToUpperInvariant();
    }
}
