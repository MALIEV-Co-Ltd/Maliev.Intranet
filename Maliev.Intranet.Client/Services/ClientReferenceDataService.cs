using System.Net.Http.Json;
using Maliev.Intranet.Shared;
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
            var countries = await _httpClient.GetFromJsonAsync<List<CountryDto>>("api/v1/ReferenceData/countries", cancellationToken);
            return countries ?? new List<CountryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading countries from BFF");
            return new List<CountryDto>();
        }
    }

    /// <summary>
    /// Gets a paginated country page from the BFF API.
    /// </summary>
    public async Task<ReferenceDataPage<CountryDto>> GetCountryPageAsync(
        string? query,
        bool includeInactive = true,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var queryPart = string.IsNullOrWhiteSpace(query) ? string.Empty : $"&query={Uri.EscapeDataString(query.Trim())}";
        var page = await _httpClient.GetFromJsonAsync<ReferenceDataPage<CountryDto>>(
            $"api/v1/ReferenceData/countries/page?pageNumber={pageNumber}&pageSize={pageSize}&includeInactive={includeInactive.ToString().ToLowerInvariant()}{queryPart}",
            cancellationToken);
        return page ?? new ReferenceDataPage<CountryDto> { PageNumber = pageNumber, PageSize = pageSize };
    }

    /// <summary>
    /// Creates a country reference record through the BFF API.
    /// </summary>
    public async Task<CountryDto> CreateCountryAsync(CountryDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/ReferenceData/countries", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CountryDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Country create returned no content.");
    }

    /// <summary>
    /// Updates a country reference record through the BFF API.
    /// </summary>
    public async Task<CountryDto> UpdateCountryAsync(Guid id, CountryDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/v1/ReferenceData/countries/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CountryDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Country update returned no content.");
    }

    /// <summary>
    /// Soft deletes a country reference record through the BFF API.
    /// </summary>
    public async Task DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/ReferenceData/countries/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Restores a soft-deleted country reference record through the BFF API.
    /// </summary>
    public async Task RestoreCountryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/v1/ReferenceData/countries/{id}/restore", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets all currencies from the BFF API.
    /// </summary>
    public async Task<List<CurrencyDto>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currencies = await _httpClient.GetFromJsonAsync<List<CurrencyDto>>("api/v1/ReferenceData/currencies", cancellationToken);
            return currencies ?? new List<CurrencyDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading currencies from BFF");
            return new List<CurrencyDto>();
        }
    }

    /// <summary>
    /// Gets a paginated currency page from the BFF API.
    /// </summary>
    public async Task<ReferenceDataPage<CurrencyDto>> GetCurrencyPageAsync(
        bool? isActive,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var activePart = isActive.HasValue ? $"&isActive={isActive.Value.ToString().ToLowerInvariant()}" : string.Empty;
        var page = await _httpClient.GetFromJsonAsync<ReferenceDataPage<CurrencyDto>>(
            $"api/v1/ReferenceData/currencies/page?pageNumber={pageNumber}&pageSize={pageSize}{activePart}",
            cancellationToken);
        return page ?? new ReferenceDataPage<CurrencyDto> { PageNumber = pageNumber, PageSize = pageSize };
    }

    /// <summary>
    /// Creates a currency reference record through the BFF API.
    /// </summary>
    public async Task<CurrencyDto> CreateCurrencyAsync(CurrencyDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/ReferenceData/currencies", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CurrencyDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Currency create returned no content.");
    }

    /// <summary>
    /// Updates a currency reference record through the BFF API.
    /// </summary>
    public async Task<CurrencyDto> UpdateCurrencyAsync(Guid id, CurrencyDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/v1/ReferenceData/currencies/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CurrencyDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Currency update returned no content.");
    }

    /// <summary>
    /// Deletes a currency reference record through the BFF API.
    /// </summary>
    public async Task DeleteCurrencyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/ReferenceData/currencies/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets the primary currency from the BFF API.
    /// </summary>
    public async Task<CurrencyDto?> GetPrimaryCurrencyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CurrencyDto>("api/v1/ReferenceData/currencies/primary", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading primary currency from BFF");
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
            var locations = await _httpClient.GetFromJsonAsync<List<RegistryThaiLocation>>($"api/v1/Customers/locations/thai?query={Uri.EscapeDataString(query)}&limit={limit}", cancellationToken);
            return locations ?? new List<RegistryThaiLocation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Thai locations via BFF for query {Query}", query);
            return new List<RegistryThaiLocation>();
        }
    }

    /// <summary>
    /// Gets a paginated Thai registry location page from the BFF API.
    /// </summary>
    public async Task<ReferenceDataPage<RegistryThaiLocation>> GetRegistryLocationPageAsync(
        string? query,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var queryPart = string.IsNullOrWhiteSpace(query) ? string.Empty : $"&query={Uri.EscapeDataString(query.Trim())}";
        var page = await _httpClient.GetFromJsonAsync<ReferenceDataPage<RegistryThaiLocation>>(
            $"api/v1/ReferenceData/locations?pageNumber={pageNumber}&pageSize={pageSize}{queryPart}",
            cancellationToken);
        return page ?? new ReferenceDataPage<RegistryThaiLocation> { PageNumber = pageNumber, PageSize = pageSize };
    }

    /// <summary>
    /// Creates a Thai registry location through the BFF API.
    /// </summary>
    public async Task<RegistryThaiLocation> CreateRegistryLocationAsync(RegistryThaiLocation request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/ReferenceData/locations", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegistryThaiLocation>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Registry location create returned no content.");
    }

    /// <summary>
    /// Updates a Thai registry location through the BFF API.
    /// </summary>
    public async Task<RegistryThaiLocation> UpdateRegistryLocationAsync(Guid id, RegistryThaiLocation request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/v1/ReferenceData/locations/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegistryThaiLocation>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Registry location update returned no content.");
    }

    /// <summary>
    /// Deletes a Thai registry location through the BFF API.
    /// </summary>
    public async Task DeleteRegistryLocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/ReferenceData/locations/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

