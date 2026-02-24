namespace Maliev.Intranet.Shared.Services;
using Maliev.Intranet.Shared.Dtos;

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
    /// Updates a country name.
    /// </summary>
    Task<CountryDto?> UpdateCountryAsync(string code, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for Thai locations by query.
    /// </summary>
    Task<List<RegistryThaiLocation>> AutocompleteLocationsAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
}

