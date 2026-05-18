using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Maliev.Intranet.Shared;
using Microsoft.Extensions.Logging;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Country microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
/// <param name="logger">The logger instance.</param>
public class CountryServiceClient(HttpClient httpClient, ILogger<CountryServiceClient> logger)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<CountryServiceClient> _logger = logger;

    /// <summary>
    /// Gets the list of available countries.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of country summaries.</returns>
    public async Task<List<CountryDto>> GetCountriesAsync(CancellationToken ct = default)
    {
        // Fetch a large page size to ensure all countries are loaded for the dropdown
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CountryPaginatedResponse<CountryDto>>("/country/v1/countries?pageSize=1000", ct);
            return response?.Data.ToList() ?? new List<CountryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load countries");
            return new List<CountryDto>();
        }
    }

    /// <summary>
    /// Gets a paginated country page for reference data management.
    /// </summary>
    /// <param name="query">Optional search query.</param>
    /// <param name="includeInactive">Whether inactive countries should be included.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A normalized reference data page.</returns>
    public async Task<ReferenceDataPage<CountryDto>> GetCountryPageAsync(
        string? query,
        bool includeInactive,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, 100);
        var url = string.IsNullOrWhiteSpace(query)
            ? $"/country/v1/countries?page={page}&pageSize={size}&includeInactive={includeInactive.ToString().ToLowerInvariant()}&sortBy=name&sortOrder=asc"
            : $"/country/v1/countries/search?query={Uri.EscapeDataString(query.Trim())}&page={page}&pageSize={size}";

        var response = await _httpClient.GetFromJsonAsync<CountryPageResponse<CountryDto>>(url, ct);
        return response?.ToReferenceDataPage() ?? new ReferenceDataPage<CountryDto> { PageNumber = page, PageSize = size };
    }

    /// <summary>
    /// Creates a country reference record through CountryService.
    /// </summary>
    public async Task<CountryDto> CreateCountryAsync(CountryDto request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/country/v1/admin/countries", ToCreateRequest(request), ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CountryDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("CountryService returned an empty create response.");
    }

    /// <summary>
    /// Patches a country reference record through CountryService.
    /// </summary>
    public async Task<CountryDto> UpdateCountryAsync(Guid id, CountryDto request, CancellationToken ct = default)
    {
        var etag = request.ETag;
        if (string.IsNullOrWhiteSpace(etag))
        {
            var current = await _httpClient.GetFromJsonAsync<CountryDto>($"/country/v1/countries/{id}", ct);
            etag = current?.ETag;
        }

        if (string.IsNullOrWhiteSpace(etag))
        {
            throw new InvalidOperationException("CountryService did not provide an ETag for update.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Patch, $"/country/v1/admin/countries/{id}")
        {
            Content = JsonContent.Create(ToPatchRequest(request))
        };
        message.Headers.TryAddWithoutValidation("If-Match", etag);

        using var response = await _httpClient.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CountryDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("CountryService returned an empty update response.");
    }

    /// <summary>
    /// Soft deletes a country reference record.
    /// </summary>
    public async Task DeleteCountryAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"/country/v1/admin/countries/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Restores a soft-deleted country reference record.
    /// </summary>
    public async Task RestoreCountryAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsync($"/country/v1/admin/countries/{id}/restore", null, ct);
        response.EnsureSuccessStatusCode();
    }

    private static object ToCreateRequest(CountryDto request) => new
    {
        iso2 = request.Code.Trim().ToUpperInvariant(),
        iso3 = (request.Iso3 ?? string.Empty).Trim().ToUpperInvariant(),
        name = request.Name.Trim(),
        officialName = string.IsNullOrWhiteSpace(request.OfficialName) ? null : request.OfficialName.Trim(),
        numericCode = string.IsNullOrWhiteSpace(request.NumericCode) ? null : request.NumericCode.Trim(),
        capital = string.IsNullOrWhiteSpace(request.Capital) ? null : request.Capital.Trim(),
        region = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim(),
        subregion = string.IsNullOrWhiteSpace(request.Subregion) ? null : request.Subregion.Trim(),
        timezones = "[]",
        borders = "[]",
        callingCodes = "[]",
        topLevelDomains = "[]",
        currencies = "{}",
        languages = "{}",
        translations = "{}",
        flags = "{}"
    };

    private static object ToPatchRequest(CountryDto request) => new
    {
        iso2 = request.Code.Trim().ToUpperInvariant(),
        iso3 = (request.Iso3 ?? string.Empty).Trim().ToUpperInvariant(),
        name = request.Name.Trim(),
        officialName = string.IsNullOrWhiteSpace(request.OfficialName) ? null : request.OfficialName.Trim(),
        numericCode = string.IsNullOrWhiteSpace(request.NumericCode) ? null : request.NumericCode.Trim(),
        capital = string.IsNullOrWhiteSpace(request.Capital) ? null : request.Capital.Trim(),
        region = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim(),
        subregion = string.IsNullOrWhiteSpace(request.Subregion) ? null : request.Subregion.Trim(),
        isActive = request.IsActive
    };
}

/// <summary>
/// Wrapper for country paginated response.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class CountryPaginatedResponse<T>
{
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public IEnumerable<T> Data { get; set; } = [];
}

/// <summary>
/// Wrapper for country paginated management responses.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
internal sealed class CountryPageResponse<T>
{
    /// <summary>The current page items.</summary>
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = [];

    /// <summary>The current page number.</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>The page size.</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>The total item count.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>The total page count.</summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>Converts the downstream page to the shared Intranet shape.</summary>
    public ReferenceDataPage<T> ToReferenceDataPage() => new()
    {
        Items = Data.ToList(),
        PageNumber = Page,
        PageSize = PageSize,
        TotalCount = TotalCount,
        TotalPages = TotalPages
    };
}
