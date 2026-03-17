using System.Text.Json.Serialization;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Currency microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class CurrencyServiceClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    /// <summary>
    /// Gets the list of available currencies.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of currency summaries.</returns>
    public async Task<List<CurrencyDto>> GetCurrenciesAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetFromJsonAsync<CurrencyPaginatedResponse>("/currency/v1/currencies?pageSize=1000", ct);
        return response?.Data.ToList() ?? [];
    }

    /// <summary>
    /// Gets a currency by its code.
    /// </summary>
    /// <param name="code">The ISO 4217 currency code.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The currency or null if not found.</returns>
    public async Task<CurrencyDto?> GetCurrencyByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<CurrencyDto>($"/currency/v1/currencies/{code}", ct);
    }

    /// <summary>
    /// Gets the primary currency.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The primary currency or null if not found.</returns>
    public async Task<CurrencyDto?> GetPrimaryCurrencyAsync(CancellationToken ct = default)
    {
        var currencies = await GetCurrenciesAsync(ct);
        return currencies.FirstOrDefault(c => c.IsPrimary);
    }
}

/// <summary>
/// Wrapper for currency paginated response.
/// </summary>
internal class CurrencyPaginatedResponse
{
    [JsonPropertyName("items")]
    public IEnumerable<CurrencyDto> Data { get; set; } = [];
}
