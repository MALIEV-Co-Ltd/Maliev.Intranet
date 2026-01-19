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
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing order summaries.</returns>
    public async Task<PagedResponse<OrderSummaryDto>?> GetOrdersAsync(int page = 1, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<OrderSummaryDto>>($"/order/v1/orders?page={page}", ct);
    }
}