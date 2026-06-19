using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

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
    /// Fetches available couriers from the BFF.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Available courier options.</returns>
    public async Task<List<ShippingCourierDto>> GetCouriersAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<MalievResponse<List<ShippingCourierDto>>>("api/v1/shipping/couriers", JsonOptions, ct);
        return response?.Data ?? [];
    }

    /// <summary>
    /// Fetches shipping rates from DeliveryService via the BFF.
    /// </summary>
    /// <param name="request">Shipping rate request with origin, destination, and package details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Available shipping rate options, or null if the request failed.</returns>
    public async Task<ShippingRateResponseDto?> GetRatesAsync(ShippingRateRequestDto request, CancellationToken ct = default)
    {
        using var httpResponse = await _http.PostAsJsonAsync("api/v1/shipping/rates", request, JsonOptions, ct);
        var response = await httpResponse.Content.ReadFromJsonAsync<MalievResponse<ShippingRateResponseDto>>(JsonOptions, ct);
        return response?.Data;
    }
}
