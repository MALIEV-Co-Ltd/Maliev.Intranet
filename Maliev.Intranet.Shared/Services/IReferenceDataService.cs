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
    /// Gets a paginated country page for reference data management.
    /// </summary>
    Task<ReferenceDataPage<CountryDto>> GetCountryPageAsync(
        string? query,
        bool includeInactive = true,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a country reference record.
    /// </summary>
    Task<CountryDto> CreateCountryAsync(CountryDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a country reference record.
    /// </summary>
    Task<CountryDto> UpdateCountryAsync(Guid id, CountryDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a country reference record.
    /// </summary>
    Task DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted country reference record.
    /// </summary>
    Task RestoreCountryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currencies.
    /// </summary>
    Task<List<CurrencyDto>> GetCurrenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated currency page for reference data management.
    /// </summary>
    Task<ReferenceDataPage<CurrencyDto>> GetCurrencyPageAsync(
        bool? isActive,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a currency reference record.
    /// </summary>
    Task<CurrencyDto> CreateCurrencyAsync(CurrencyDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a currency reference record.
    /// </summary>
    Task<CurrencyDto> UpdateCurrencyAsync(Guid id, CurrencyDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a currency reference record.
    /// </summary>
    Task DeleteCurrencyAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary currency.
    /// </summary>
    Task<CurrencyDto?> GetPrimaryCurrencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for Thai locations by query.
    /// </summary>
    Task<List<RegistryThaiLocation>> AutocompleteLocationsAsync(string query, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated Thai address page for registry data management.
    /// </summary>
    Task<ReferenceDataPage<RegistryThaiLocation>> GetRegistryLocationPageAsync(
        string? query,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Thai registry location.
    /// </summary>
    Task<RegistryThaiLocation> CreateRegistryLocationAsync(RegistryThaiLocation request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a Thai registry location.
    /// </summary>
    Task<RegistryThaiLocation> UpdateRegistryLocationAsync(Guid id, RegistryThaiLocation request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a Thai registry location.
    /// </summary>
    Task DeleteRegistryLocationAsync(Guid id, CancellationToken cancellationToken = default);
}

