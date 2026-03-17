using System.Text.Json;
using Maliev.Intranet.Shared;


namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Order microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class OrderServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of orders.
    /// </summary>
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing order summaries.</returns>
    public async Task<PagedResponse<OrderSummaryDto>?> GetOrdersAsync(Guid? customerId = null, int page = 1, CancellationToken ct = default)
    {
        var url = $"/order/v1/orders?page={page}";
        if (customerId.HasValue) url += $"&customerId={customerId.Value}";
        return await httpClient.GetFromJsonAsync<PagedResponse<OrderSummaryDto>>(url, ct);
    }

    /// <summary>
    /// Retrieves order metrics.
    /// </summary>
    public async Task<int> GetActiveOrderCountAsync(CancellationToken ct = default)
    {
        var httpResponse = await httpClient.GetAsync("/order/v1/metrics/active-count", ct);
        if (!httpResponse.IsSuccessStatusCode) return 0;
        var response = await httpResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return response.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }

    /// <summary>
    /// Retrieves detailed information for a single order by ID.
    /// Returns <c>null</c> when the order is not found (404).
    /// </summary>
    public async Task<OrderDetailDto?> GetOrderByIdAsync(string id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/order/v1/orders/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<OrderDetailDto>(ct);
    }

    /// <summary>
    /// Updates the status of an order.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateStatusAsync(string id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        return await httpClient.PatchAsJsonAsync($"/order/v1/orders/{id}/status", request, ct);
    }

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateOrderAsync(string id, UpdateOrderRequest request, CancellationToken ct = default)
    {
        return await httpClient.PutAsJsonAsync($"/order/v1/orders/{id}", request, ct);
    }

    /// <summary>
    /// Retrieves the count of orders with status OnHold or Delayed.
    /// </summary>
    public async Task<int> GetOnHoldOrderCountAsync(CancellationToken ct = default)
    {
        var httpResponse = await httpClient.GetAsync("/order/v1/metrics/on-hold-count", ct);
        if (!httpResponse.IsSuccessStatusCode) return 0;
        var response = await httpResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
        return response.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }
}

