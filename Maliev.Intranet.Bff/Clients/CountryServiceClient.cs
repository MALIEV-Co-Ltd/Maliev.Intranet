using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Country microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class CountryServiceClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

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
            Console.WriteLine($"[CountryServiceClient] Failed to load countries: {ex.Message}");
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
