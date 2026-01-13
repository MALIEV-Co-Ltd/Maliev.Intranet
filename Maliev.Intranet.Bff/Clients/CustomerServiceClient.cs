using System.Net.Http.Json;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Customer microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class CustomerServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of customers, optionally filtered by a search query.
    /// </summary>
    /// <param name="query">The search query to filter customers.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing customer summaries.</returns>
    public async Task<PagedResponse<CustomerSummaryDto>?> GetCustomersAsync(string? query = null, int page = 1, CancellationToken ct = default)
    {
        var url = $"/customer/v1/customers?page={page}";
        if (!string.IsNullOrEmpty(query)) url += $"&query={Uri.EscapeDataString(query)}";

        return await httpClient.GetFromJsonAsync<PagedResponse<CustomerSummaryDto>>(url, ct);
    }
}