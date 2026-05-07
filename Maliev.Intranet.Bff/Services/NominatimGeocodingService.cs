using System.Globalization;
using System.Text.Json.Serialization;
using Maliev.Intranet.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Geocodes explicitly selected addresses for map display using Nominatim.
/// </summary>
public sealed class NominatimGeocodingService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    ILogger<NominatimGeocodingService> logger)
{
    private static readonly SemaphoreSlim RateLimitLock = new(1, 1);
    private static DateTimeOffset _lastRequestUtc = DateTimeOffset.MinValue;

    /// <summary>
    /// Geocodes an address query and returns a displayable map position when available.
    /// </summary>
    /// <param name="query">The address query to geocode.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The geocoded address, or <c>null</c> when no match is available.</returns>
    public async Task<AddressGeocodeResponse?> GeocodeAsync(string query, CancellationToken ct = default)
    {
        var normalizedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return null;
        }

        var cacheKey = $"nominatim:address:{normalizedQuery.ToUpperInvariant()}";
        if (cache.TryGetValue(cacheKey, out AddressGeocodeResponse? cached))
        {
            return cached;
        }

        await RateLimitLock.WaitAsync(ct);
        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRequestUtc;
            if (elapsed < TimeSpan.FromSeconds(1))
            {
                await Task.Delay(TimeSpan.FromSeconds(1) - elapsed, ct);
            }

            var client = httpClientFactory.CreateClient("Nominatim");
            var url = $"search?format=jsonv2&limit=1&addressdetails=1&q={Uri.EscapeDataString(normalizedQuery)}";
            var results = await client.GetFromJsonAsync<List<NominatimSearchResult>>(url, ct) ?? [];
            _lastRequestUtc = DateTimeOffset.UtcNow;

            var first = results.FirstOrDefault();
            if (first is null ||
                !double.TryParse(first.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(first.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                return null;
            }

            var response = new AddressGeocodeResponse
            {
                Latitude = latitude,
                Longitude = longitude,
                DisplayName = first.DisplayName ?? normalizedQuery
            };

            cache.Set(cacheKey, response, TimeSpan.FromDays(7));
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Nominatim geocode failed for address query {Query}", normalizedQuery);
            return null;
        }
        finally
        {
            RateLimitLock.Release();
        }
    }

    private sealed class NominatimSearchResult
    {
        [JsonPropertyName("lat")]
        public string? Latitude { get; set; }

        [JsonPropertyName("lon")]
        public string? Longitude { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}
