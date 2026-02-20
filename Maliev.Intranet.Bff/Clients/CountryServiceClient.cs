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
