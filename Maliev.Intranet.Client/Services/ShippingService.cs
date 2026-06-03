using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Client-side service for fetching shipping rates from the BFF.
/// </summary>
public class ShippingService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingService"/> class.
    /// </summary>
    /// <param name="http">The HTTP client for BFF communication.</param>
    public ShippingService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Fetches shipping rates from DHL Express via the BFF.
    /// </summary>
    /// <param name="request">Shipping rate request with origin, destination, and package details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Available shipping rate options, or null if the request failed.</returns>
    public async Task<ShippingRateResponseDto?> GetRatesAsync(ShippingRateRequestDto request, CancellationToken ct = default)
    {
        var query = $"api/v1/shipping/rates" +
                    $"?originCountry={Uri.EscapeDataString(request.OriginCountryCode)}" +
                    $"&destCountry={Uri.EscapeDataString(request.DestinationCountryCode)}" +
                    $"&weightKg={request.WeightKg}";

        if (!string.IsNullOrWhiteSpace(request.DestinationPostalCode))
            query += $"&destPostalCode={Uri.EscapeDataString(request.DestinationPostalCode)}";
        if (request.LengthCm.HasValue)
            query += $"&lengthCm={request.LengthCm.Value}";
        if (request.WidthCm.HasValue)
            query += $"&widthCm={request.WidthCm.Value}";
        if (request.HeightCm.HasValue)
            query += $"&heightCm={request.HeightCm.Value}";

        var response = await _http.GetFromJsonAsync<MalievResponse<ShippingRateResponseDto>>(query, JsonOptions, ct);
        return response?.Data;
    }
}
