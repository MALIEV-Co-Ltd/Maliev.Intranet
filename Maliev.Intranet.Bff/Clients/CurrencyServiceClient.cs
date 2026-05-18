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
    /// Gets a paginated currency page for reference data management.
    /// </summary>
    /// <param name="isActive">Optional active-status filter.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A normalized reference data page.</returns>
    public async Task<ReferenceDataPage<CurrencyDto>> GetCurrencyPageAsync(
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, 200);
        var activeQuery = isActive.HasValue ? $"&isActive={isActive.Value.ToString().ToLowerInvariant()}" : string.Empty;
        var response = await _httpClient.GetFromJsonAsync<CurrencyPaginatedResponse>(
            $"/currency/v1/currencies?page={page}&pageSize={size}{activeQuery}", ct);

        return response?.ToReferenceDataPage() ?? new ReferenceDataPage<CurrencyDto> { PageNumber = page, PageSize = size };
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
    /// Gets a currency by its unique identifier using the admin endpoint.
    /// </summary>
    public async Task<CurrencyDto?> GetCurrencyByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<CurrencyDto>($"/currency/v1/admin/currencies/{id}", ct);
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
    /// Creates a currency reference record.
    /// </summary>
    public async Task<CurrencyDto> CreateCurrencyAsync(CurrencyDto request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/currency/v1/admin/currencies", ToCreateRequest(request), ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CurrencyDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("CurrencyService returned an empty create response.");
    }

    /// <summary>
    /// Updates a currency reference record.
    /// </summary>
    public async Task<CurrencyDto> UpdateCurrencyAsync(Guid id, CurrencyDto request, CancellationToken ct = default)
    {
        var etag = request.ETag;
        if (string.IsNullOrWhiteSpace(etag))
        {
            etag = (await GetCurrencyByIdAsync(id, ct))?.ETag;
        }

        if (string.IsNullOrWhiteSpace(etag))
        {
            throw new InvalidOperationException("CurrencyService did not provide an ETag for update.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Put, $"/currency/v1/admin/currencies/{id}")
        {
            Content = JsonContent.Create(ToUpdateRequest(request))
        };
        message.Headers.TryAddWithoutValidation("If-Match", etag);

        using var response = await _httpClient.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CurrencyDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("CurrencyService returned an empty update response.");
    }

    /// <summary>
    /// Deletes a currency reference record.
    /// </summary>
    public async Task DeleteCurrencyAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"/currency/v1/admin/currencies/{id}", ct);
        response.EnsureSuccessStatusCode();
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

    private static object ToCreateRequest(CurrencyDto request) => new
    {
        code = request.Code.Trim().ToUpperInvariant(),
        name = request.Name.Trim(),
        symbol = request.Symbol.Trim(),
        decimalPlaces = request.DecimalPlaces,
        isActive = request.IsActive
    };

    private static object ToUpdateRequest(CurrencyDto request) => new
    {
        name = request.Name.Trim(),
        symbol = request.Symbol.Trim(),
        decimalPlaces = request.DecimalPlaces,
        isActive = request.IsActive
    };
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

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    public ReferenceDataPage<CurrencyDto> ToReferenceDataPage() => new()
    {
        Items = Data.ToList(),
        PageNumber = Page,
        PageSize = PageSize,
        TotalCount = TotalCount,
        TotalPages = TotalPages
    };
}
