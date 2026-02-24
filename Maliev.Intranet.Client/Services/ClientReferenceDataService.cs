using System.Net.Http.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Shared.Services;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Client-side implementation of IReferenceDataService that calls the BFF API.
/// </summary>
public class ClientReferenceDataService : IReferenceDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClientReferenceDataService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientReferenceDataService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for BFF communication.</param>
    /// <param name="logger">Logger instance.</param>
    public ClientReferenceDataService(HttpClient httpClient, ILogger<ClientReferenceDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets all countries from the BFF API.
    /// </summary>
    public async Task<List<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var countries = await _httpClient.GetFromJsonAsync<List<CountryDto>>("api/ReferenceData/countries", cancellationToken);
            return countries ?? new List<CountryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading countries from BFF");
            return new List<CountryDto>();
        }
    }

    /// <summary>
    /// Updates a country name via the BFF API.
    /// </summary>
    public async Task<CountryDto?> UpdateCountryAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { Name = name };
            var response = await _httpClient.PatchAsJsonAsync($"api/ReferenceData/countries/{code}", payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CountryDto>(cancellationToken);
            }
            _logger.LogError("Failed to update country {Code}. Status: {StatusCode}", code, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating country {Code}", code);
            return null;
        }
    }

    /// <summary>
    /// Searches for Thai locations by query via the BFF API.
    /// </summary>
    public async Task<List<RegistryThaiLocation>> AutocompleteLocationsAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var locations = await _httpClient.GetFromJsonAsync<List<RegistryThaiLocation>>($"api/Customers/locations/thai?query={Uri.EscapeDataString(query)}&limit={limit}", cancellationToken);
            return locations ?? new List<RegistryThaiLocation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Thai locations via BFF for query {Query}", query);
            return new List<RegistryThaiLocation>();
        }
    }
}

