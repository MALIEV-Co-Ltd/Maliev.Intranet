using Maliev.Intranet.Shared.Dtos;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Typed HTTP client for DHL Express MyDHL API Rating service.
/// </summary>
public class DhlExpressClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DhlExpressClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Initializes a new instance of the <see cref="DhlExpressClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured with DHL base address.</param>
    /// <param name="configuration">
    /// Application configuration. Credentials are read from the <c>DhlExpress</c> section
    /// (e.g. <c>DhlExpress:Username</c>, <c>DhlExpress:Password</c>, <c>DhlExpress:AccountNumber</c>).
    /// In production these should be injected as environment variables
    /// (<c>DhlExpress__Username</c>, <c>DhlExpress__Password</c>, <c>DhlExpress__AccountNumber</c>)
    /// sourced from GCP Secret Manager.
    /// </param>
    /// <param name="logger">Logger instance.</param>
    public DhlExpressClient(HttpClient httpClient, IConfiguration configuration, ILogger<DhlExpressClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var user = configuration["DhlExpress:Username"];
        var key = configuration["DhlExpress:Password"];
        var acct = configuration["DhlExpress:AccountNumber"];

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(acct))
        {
            _logger.LogWarning("DHL Express credentials are not configured. " +
                "Set DhlExpress:Username, DhlExpress:Password, and DhlExpress:AccountNumber " +
                "in appsettings or as environment variables (DhlExpress__Username, etc.).");
        }
    }

    /// <summary>
    /// Retrieves shipping rates from DHL Express for the given package details.
    /// </summary>
    /// <param name="request">Shipping rate request with origin, destination, and package details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Available shipping rate options.</returns>
    public async Task<ShippingRateResponseDto> GetRatesAsync(ShippingRateRequestDto request, CancellationToken ct = default)
    {
        SetAuthHeader();

        var payload = new
        {
            accountNumber = _configuration["DhlExpress:AccountNumber"] ?? "",
            originCountryCode = request.OriginCountryCode,
            destinationCountryCode = request.DestinationCountryCode,
            destinationPostalCode = request.DestinationPostalCode ?? "",
            packages = new[]
            {
                new
                {
                    weight = new { value = (double)request.WeightKg, unit = "KG" },
                    dimensions = request.LengthCm.HasValue && request.WidthCm.HasValue && request.HeightCm.HasValue
                        ? new { length = (double)request.LengthCm.Value, width = (double)request.WidthCm.Value, height = (double)request.HeightCm.Value, unit = "CM" }
                        : null
                }
            }
        };

        _logger.LogInformation("Fetching DHL rates: {Origin} -> {Dest}, {Weight}kg",
            request.OriginCountryCode, request.DestinationCountryCode, request.WeightKg);

        var response = await _httpClient.PostAsJsonAsync("/rates", payload, JsonOptions, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("DHL API error {Status}: {Body}", (int)response.StatusCode, errorBody);
            throw new HttpRequestException($"DHL API returned {(int)response.StatusCode}: {errorBody}");
        }

        var dhlResponse = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        return MapRates(dhlResponse);
    }

    private void SetAuthHeader()
    {
        var username = _configuration["DhlExpress:Username"] ?? "";
        var password = _configuration["DhlExpress:Password"] ?? "";
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    private static ShippingRateResponseDto MapRates(JsonElement dhlResponse)
    {
        var result = new ShippingRateResponseDto();

        if (!dhlResponse.TryGetProperty("products", out var products))
        {
            return result;
        }

        foreach (var product in products.EnumerateArray())
        {
            var totalPrice = 0m;
            if (product.TryGetProperty("totalPrice", out var priceElements) && priceElements.ValueKind == JsonValueKind.Array)
            {
                foreach (var price in priceElements.EnumerateArray())
                {
                    if (price.TryGetProperty("price", out var priceValue) && priceValue.ValueKind == JsonValueKind.Number)
                    {
                        totalPrice += priceValue.GetDecimal();
                    }
                }
            }

            var currencyCode = "THB";
            if (product.TryGetProperty("totalPrice", out var priceArr) && priceArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var price in priceArr.EnumerateArray())
                {
                    if (price.TryGetProperty("priceCurrency", out var curr) && curr.ValueKind == JsonValueKind.String)
                    {
                        currencyCode = curr.GetString() ?? "THB";
                        break;
                    }
                }
            }

            var productName = "DHL Express";
            if (product.TryGetProperty("productName", out var name) && name.ValueKind == JsonValueKind.String)
            {
                productName = name.GetString() ?? productName;
            }

            string? edd = null;
            if (product.TryGetProperty("estimatedDeliveryDate", out var date) && date.ValueKind == JsonValueKind.String)
            {
                edd = date.GetString();
            }

            result.Rates.Add(new ShippingRateOptionDto
            {
                ProductName = productName,
                TotalPrice = totalPrice,
                CurrencyCode = currencyCode,
                EstimatedDeliveryDate = edd
            });
        }

        return result;
    }
}
