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

    /// <summary>
    /// Gets the live exchange rate between two currencies.
    /// Returns null if the rate is unavailable.
    /// </summary>
    /// <param name="from">Source currency code (e.g. "THB").</param>
    /// <param name="to">Target currency code (e.g. "USD").</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The exchange rate, or null if unavailable.</returns>
    public async Task<decimal?> GetExchangeRateAsync(string from, string to, CancellationToken ct = default)
    {
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return 1m;

        try
        {
            var response = await _httpClient.GetFromJsonAsync<ExchangeRateResult>(
                $"/currency/v1/rates?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}&mode=live", ct);
            return response?.Rate;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Minimal projection of the CurrencyService ExchangeRateResponse for rate lookup.
/// </summary>
internal sealed class ExchangeRateResult
{
    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }
}

/// <summary>
/// Wrapper for currency paginated response.
/// </summary>
internal class CurrencyPaginatedResponse
{
    [JsonPropertyName("items")]
    public IEnumerable<CurrencyDto> Data { get; set; } = [];
}
