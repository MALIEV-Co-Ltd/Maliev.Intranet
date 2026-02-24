using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Registry microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class RegistryServiceClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    /// <summary>
    /// Searches for Thai locations by query.
    /// </summary>
    public virtual async Task<List<RegistryThaiLocation>> AutocompleteLocationsAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var response = await _httpClient.GetFromJsonAsync<RegistryApiResponse<IEnumerable<RegistryThaiLocation>>>($"/registry/v1/thai/addresses/autocomplete?query={Uri.EscapeDataString(query)}&limit={limit}", options, ct);
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
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var response = await _httpClient.GetFromJsonAsync<RegistryApiResponse<IEnumerable<RegistryThaiLocation>>>(
            $"/registry/v1/thai/addresses/autocomplete-multi?{query}", options, ct);

        return response?.Data?.ToList() ?? [];
    }

    /// <summary>
    /// Searches for Thai companies by name or tax ID.
    /// </summary>
    public virtual async Task<List<RegistryCompanyProfile>> SearchCompaniesAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var response = await _httpClient.GetFromJsonAsync<RegistryApiResponse<IEnumerable<RegistryCompanyProfile>>>($"/registry/v1/thai/companies/search?query={Uri.EscapeDataString(query)}&limit={limit}", options, ct);
        return response?.Data?.ToList() ?? new List<RegistryCompanyProfile>();
    }
}
