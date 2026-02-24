using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Currency microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class CurrencyServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of currencies.
    /// </summary>
    public async Task<PagedResponse<CurrencyDto>?> GetCurrenciesAsync(int page = 1, int pageSize = 50, bool? isActive = null, CancellationToken ct = default)
    {
        var url = $"/currency/v1/currencies?page={page}&pageSize={pageSize}";
        if (isActive.HasValue) url += $"&isActive={isActive.Value}";

        var response = await httpClient.GetFromJsonAsync<PaginatedCurrencyResponse>(url, ct);
        if (response == null) return null;

        return new PagedResponse<CurrencyDto>
        {
            Data = response.Items,
            Meta = new PaginationMeta
            {
                CurrentPage = response.Page,
                PageSize = response.PageSize,
                TotalCount = response.TotalCount,
                TotalPages = (int)Math.Ceiling(response.TotalCount / (double)response.PageSize)
            }
        };
    }

    /// <summary>
    /// Creates a new currency.
    /// </summary>
    public async Task<CurrencyDto?> CreateCurrencyAsync(CreateCurrencyRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/currency/v1/admin/currencies", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CurrencyDto>(cancellationToken: ct);
        }
        return null;
    }

    private class PaginatedCurrencyResponse
    {
        public List<CurrencyDto> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}

/// <summary>
/// Request to create a new currency.
/// </summary>
public class CreateCurrencyRequest
{
    /// <summary>Gets or sets the currency code (e.g. USD).</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the currency name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the currency symbol.</summary>
    public string Symbol { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of decimal places.</summary>
    public int DecimalPlaces { get; set; } = 2;
}
