using System.Text.Json.Serialization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Shared.Services;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Service for accessing reference data (countries, etc.) from Blazor components.
/// Uses service account authentication for public data and user context for registry data.
/// </summary>
public class ReferenceDataService : IReferenceDataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RegistryServiceClient _registryClient;
    private readonly ILogger<ReferenceDataService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="registryClient">Registry service client.</param>
    /// <param name="logger">Logger instance.</param>
    public ReferenceDataService(IHttpClientFactory httpClientFactory, RegistryServiceClient registryClient, ILogger<ReferenceDataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _registryClient = registryClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets all countries using service account authentication.
    /// </summary>
    public async Task<List<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CountryServiceAccount");
            // Request a large page size to ensure we get all countries for dropdowns
            var response = await client.GetAsync("/country/v1/countries?pageSize=1000", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch countries from Country Service. Status: {StatusCode}", response.StatusCode);
                return new List<CountryDto>();
            }

            var countryResponse = await response.Content.ReadFromJsonAsync<CountryPaginatedResponse<CountryDto>>(cancellationToken);
            var countries = countryResponse?.Data.ToList() ?? new List<CountryDto>();

            _logger.LogInformation("Fetched {Count} countries from Country Service", countries.Count);

            return countries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading countries from Country Service");
            return new List<CountryDto>();
        }
    }

    /// <summary>
    /// Updates a country name using service account authentication.
    /// </summary>
    public async Task<CountryDto?> UpdateCountryAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CountryServiceAccount");
            var payload = new { Name = name };
            var response = await client.PatchAsJsonAsync($"/country/v1/admin/countries/{code}", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update country {Code}. Status: {StatusCode}", code, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<CountryDto>(cancellationToken);
            _logger.LogInformation("Updated country {Code} to name {Name}", code, name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating country {Code}", code);
            return null;
        }
    }

    /// <summary>
    /// Searches for Thai locations by query using the user's context.
    /// </summary>
    public async Task<List<RegistryThaiLocation>> AutocompleteLocationsAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _registryClient.AutocompleteLocationsAsync(query, limit, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error autocompleting Thai locations from Registry Service for query {Query}", query);
            return new List<RegistryThaiLocation>();
        }
    }
}

/// <summary>
/// Wrapper for country paginated response from Country Service.
/// </summary>
internal class CountryPaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = [];
}
