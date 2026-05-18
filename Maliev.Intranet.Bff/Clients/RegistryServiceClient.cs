using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Registry microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class RegistryServiceClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Gets a paginated Thai location page for registry data management.
    /// </summary>
    public virtual async Task<ReferenceDataPage<RegistryThaiLocation>> GetLocationPageAsync(
        string? query,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, 100);
        var queryString = string.IsNullOrWhiteSpace(query) ? string.Empty : $"&query={Uri.EscapeDataString(query.Trim())}";
        var response = await _httpClient.GetFromJsonAsync<RegistryApiResponse<RegistryLocationPageResponse>>(
            $"/registry/v1/thai/addresses?pageNumber={page}&pageSize={size}{queryString}", JsonOptions, ct);

        return response?.Data?.ToReferenceDataPage() ?? new ReferenceDataPage<RegistryThaiLocation> { PageNumber = page, PageSize = size };
    }

    /// <summary>
    /// Searches for Thai locations by query.
    /// </summary>
    public virtual async Task<List<RegistryThaiLocation>> AutocompleteLocationsAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var response = await _httpClient.GetFromJsonAsync<RegistryApiResponse<IEnumerable<RegistryThaiLocation>>>($"/registry/v1/thai/addresses/autocomplete?query={Uri.EscapeDataString(query)}&limit={limit}", JsonOptions, ct);
        return response?.Data?.ToList() ?? new List<RegistryThaiLocation>();
    }

    /// <summary>
    /// Searches for Thai locations using multi-field matching with weighted similarity scoring.
    /// Provides best matches using all available address components.
    /// </summary>
    /// <param name="postalCode">Postal code (5 digits).</param>
    /// <param name="district">Sub-district name (ตำบล/แขวง).</param>
    /// <param name="city">District/city name (อำเภอ/เขต).</param>
    /// <param name="province">Province name (จังหวัด).</param>
    /// <param name="limit">Maximum number of results (default: 3).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Locations ranked by composite similarity score.</returns>
    public virtual async Task<List<RegistryThaiLocation>> AutocompleteLocationsMultiFieldAsync(
        string? postalCode,
        string? district,
        string? city,
        string? province,
        int limit = 3,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(postalCode))
            queryParams.Add($"postalCode={Uri.EscapeDataString(postalCode)}");
        if (!string.IsNullOrWhiteSpace(district))
            queryParams.Add($"district={Uri.EscapeDataString(district)}");
        if (!string.IsNullOrWhiteSpace(city))
            queryParams.Add($"city={Uri.EscapeDataString(city)}");
        if (!string.IsNullOrWhiteSpace(province))
            queryParams.Add($"province={Uri.EscapeDataString(province)}");

        queryParams.Add($"limit={limit}");

        var query = string.Join("&", queryParams);
        var response = await _httpClient.GetFromJsonAsync<RegistryApiResponse<IEnumerable<RegistryThaiLocation>>>(
            $"/registry/v1/thai/addresses/autocomplete-multi?{query}", JsonOptions, ct);

        return response?.Data?.ToList() ?? [];
    }

    /// <summary>
    /// Creates a Thai registry location.
    /// </summary>
    public virtual async Task<RegistryThaiLocation> CreateLocationAsync(RegistryThaiLocation request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/registry/v1/thai/addresses", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RegistryApiResponse<RegistryThaiLocation>>(JsonOptions, ct);
        return result?.Data ?? throw new InvalidOperationException("RegistryService returned an empty create response.");
    }

    /// <summary>
    /// Updates a Thai registry location.
    /// </summary>
    public virtual async Task<RegistryThaiLocation> UpdateLocationAsync(Guid id, RegistryThaiLocation request, CancellationToken ct = default)
    {
        request.Id = id;
        using var response = await _httpClient.PutAsJsonAsync($"/registry/v1/thai/addresses/{id}", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RegistryApiResponse<RegistryThaiLocation>>(JsonOptions, ct);
        return result?.Data ?? throw new InvalidOperationException("RegistryService returned an empty update response.");
    }

    /// <summary>
    /// Deletes a Thai registry location.
    /// </summary>
    public virtual async Task DeleteLocationAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"/registry/v1/thai/addresses/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Searches for Thai companies by name or tax ID.
    /// </summary>
    public virtual async Task<List<RegistryCompanyProfile>> SearchCompaniesAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var response = await _httpClient.GetFromJsonAsync<RegistryApiResponse<IEnumerable<RegistryCompanyProfile>>>($"/registry/v1/thai/companies/search?query={Uri.EscapeDataString(query)}&limit={limit}", JsonOptions, ct);
        return response?.Data?.ToList() ?? new List<RegistryCompanyProfile>();
    }
}

/// <summary>
/// Downstream RegistryService page response for Thai locations.
/// </summary>
internal sealed class RegistryLocationPageResponse
{
    /// <summary>The location items in the current page.</summary>
    [JsonPropertyName("items")]
    public List<RegistryThaiLocation> Items { get; set; } = [];

    /// <summary>The current page number.</summary>
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    /// <summary>The current page size.</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>The total number of matching locations.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>The total number of pages.</summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>Converts the downstream page into the shared Intranet shape.</summary>
    public ReferenceDataPage<RegistryThaiLocation> ToReferenceDataPage() => new()
    {
        Items = Items,
        PageNumber = PageNumber,
        PageSize = PageSize,
        TotalCount = TotalCount,
        TotalPages = TotalPages
    };
}
