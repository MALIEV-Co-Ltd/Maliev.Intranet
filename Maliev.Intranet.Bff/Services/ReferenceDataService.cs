using System.Text.Json.Serialization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Service for accessing reference data (countries, etc.) from Blazor components.
/// Uses service account authentication for public data and user context for registry data.
/// </summary>
public class ReferenceDataService : IReferenceDataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CountryServiceClient _countryClient;
    private readonly RegistryServiceClient _registryClient;
    private readonly CurrencyServiceClient _currencyClient;
    private readonly ILogger<ReferenceDataService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="countryClient">Country service client.</param>
    /// <param name="registryClient">Registry service client.</param>
    /// <param name="currencyClient">Currency service client.</param>
    /// <param name="logger">Logger instance.</param>
    public ReferenceDataService(
        IHttpClientFactory httpClientFactory,
        CountryServiceClient countryClient,
        RegistryServiceClient registryClient,
        CurrencyServiceClient currencyClient,
        ILogger<ReferenceDataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _countryClient = countryClient;
        _registryClient = registryClient;
        _currencyClient = currencyClient;
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
            var response = await client.GetAsync("/country/v1/countries?pageSize=1000", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to fetch countries from Country Service. Status: {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    error);
                throw new HttpRequestException(
                    $"Country Service returned {(int)response.StatusCode} {response.ReasonPhrase}: {error}",
                    null,
                    response.StatusCode);
            }

            var countryResponse = await response.Content.ReadFromJsonAsync<CountryPaginatedResponse<CountryDto>>(cancellationToken);
            var countries = countryResponse?.Data.ToList() ?? [];
            if (countries.Count == 0)
            {
                throw new InvalidOperationException("Country Service returned no countries. Address workflows require seeded country reference data.");
            }

            _logger.LogInformation("Fetched {Count} countries from Country Service", countries.Count);

            return countries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading countries from Country Service");
            throw;
        }
    }

    /// <summary>
    /// Gets a paginated country page for reference data management.
    /// </summary>
    public Task<ReferenceDataPage<CountryDto>> GetCountryPageAsync(
        string? query,
        bool includeInactive = true,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
        => _countryClient.GetCountryPageAsync(query, includeInactive, pageNumber, pageSize, cancellationToken);

    /// <summary>
    /// Creates a country reference record.
    /// </summary>
    public Task<CountryDto> CreateCountryAsync(CountryDto request, CancellationToken cancellationToken = default)
        => _countryClient.CreateCountryAsync(request, cancellationToken);

    /// <summary>
    /// Updates a country reference record.
    /// </summary>
    public Task<CountryDto> UpdateCountryAsync(Guid id, CountryDto request, CancellationToken cancellationToken = default)
        => _countryClient.UpdateCountryAsync(id, request, cancellationToken);

    /// <summary>
    /// Soft deletes a country reference record.
    /// </summary>
    public Task DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default)
        => _countryClient.DeleteCountryAsync(id, cancellationToken);

    /// <summary>
    /// Restores a soft-deleted country reference record.
    /// </summary>
    public Task RestoreCountryAsync(Guid id, CancellationToken cancellationToken = default)
        => _countryClient.RestoreCountryAsync(id, cancellationToken);

    /// <summary>
    /// Gets all currencies.
    /// </summary>
    public async Task<List<CurrencyDto>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currencies = await _currencyClient.GetCurrenciesAsync(cancellationToken);
            _logger.LogInformation("Fetched {Count} currencies from Currency Service", currencies.Count);
            return currencies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading currencies from Currency Service");
            return new List<CurrencyDto>();
        }
    }

    /// <summary>
    /// Gets a paginated currency page for reference data management.
    /// </summary>
    public Task<ReferenceDataPage<CurrencyDto>> GetCurrencyPageAsync(
        bool? isActive,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
        => _currencyClient.GetCurrencyPageAsync(isActive, pageNumber, pageSize, cancellationToken);

    /// <summary>
    /// Creates a currency reference record.
    /// </summary>
    public Task<CurrencyDto> CreateCurrencyAsync(CurrencyDto request, CancellationToken cancellationToken = default)
        => _currencyClient.CreateCurrencyAsync(request, cancellationToken);

    /// <summary>
    /// Updates a currency reference record.
    /// </summary>
    public Task<CurrencyDto> UpdateCurrencyAsync(Guid id, CurrencyDto request, CancellationToken cancellationToken = default)
        => _currencyClient.UpdateCurrencyAsync(id, request, cancellationToken);

    /// <summary>
    /// Deletes a currency reference record.
    /// </summary>
    public Task DeleteCurrencyAsync(Guid id, CancellationToken cancellationToken = default)
        => _currencyClient.DeleteCurrencyAsync(id, cancellationToken);

    /// <summary>
    /// Gets the primary currency.
    /// </summary>
    public async Task<CurrencyDto?> GetPrimaryCurrencyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _currencyClient.GetPrimaryCurrencyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading primary currency from Currency Service");
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

    /// <summary>
    /// Gets a paginated Thai address page for registry data management.
    /// </summary>
    public Task<ReferenceDataPage<RegistryThaiLocation>> GetRegistryLocationPageAsync(
        string? query,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
        => _registryClient.GetLocationPageAsync(query, pageNumber, pageSize, cancellationToken);

    /// <summary>
    /// Creates a Thai registry location.
    /// </summary>
    public Task<RegistryThaiLocation> CreateRegistryLocationAsync(RegistryThaiLocation request, CancellationToken cancellationToken = default)
        => _registryClient.CreateLocationAsync(request, cancellationToken);

    /// <summary>
    /// Updates a Thai registry location.
    /// </summary>
    public Task<RegistryThaiLocation> UpdateRegistryLocationAsync(Guid id, RegistryThaiLocation request, CancellationToken cancellationToken = default)
        => _registryClient.UpdateLocationAsync(id, request, cancellationToken);

    /// <summary>
    /// Deletes a Thai registry location.
    /// </summary>
    public Task DeleteRegistryLocationAsync(Guid id, CancellationToken cancellationToken = default)
        => _registryClient.DeleteLocationAsync(id, cancellationToken);
}

/// <summary>
/// Wrapper for country paginated response from Country Service.
/// </summary>
internal class CountryPaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = [];
}
