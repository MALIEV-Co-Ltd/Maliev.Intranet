namespace Maliev.Intranet.Shared.Services;

/// <summary>
/// Service interface for accessing reference data.
/// </summary>
public interface IReferenceDataService
{
    /// <summary>
    /// Gets all countries.
    /// </summary>
    Task<List<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currencies.
    /// </summary>
    Task<List<CurrencyDto>> GetCurrenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary currency.
    /// </summary>
    Task<CurrencyDto?> GetPrimaryCurrencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for Thai locations by query.
    /// </summary>
    Task<List<RegistryThaiLocation>> AutocompleteLocationsAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
}

