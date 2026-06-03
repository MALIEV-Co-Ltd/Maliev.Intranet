# DHL Express Rate Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate DHL Express MyDHL API v3.2.2 Rating service via BFF proxy to fetch real-time shipping rates in QuoteSummaryBar.

**Architecture:** BFF proxy pattern — `DhlExpressClient` typed HttpClient calls DHL API directly (external service, no Aspire service discovery). New `ShippingController` exposes `GET /intranet/v1/shipping/rates`. Client-side `ShippingService` calls the BFF endpoint. "Get DHL Rate" button in `QuoteSummaryBar` triggers fetch and auto-populates `ShippingCost`.

**Tech Stack:** ASP.NET Core, Blazor WASM, MudBlazor, `IHttpClientFactory`, Basic Auth, C# 13

---

### Task 1: Add shipping permission and DTOs

**Files:**
- Modify: `Maliev.Intranet.Shared/Dtos/MalievPermissions.cs`
- Create: `Maliev.Intranet.Shared/Dtos/ShippingDtos.cs`

- [ ] **Step 1: Add `Shipping` permission class to MalievPermissions.cs**

Add before the closing `}` of the `MalievPermissions` class (after `Job` class at line 697):

```csharp
    /// <summary>
    /// Permissions for shipping rate calculation and carrier integration.
    /// </summary>
    public static class Shipping
    {
        /// <summary>Permission to retrieve shipping rates from carrier APIs.</summary>
        public const string RatesRead = "shipping.rates.read";
    }
```

- [ ] **Step 2: Create ShippingDtos.cs**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Request to fetch shipping rates from a carrier.
/// </summary>
public record ShippingRateRequestDto
{
    /// <summary>Origin country ISO code (e.g. "TH").</summary>
    [Required]
    public string OriginCountryCode { get; init; } = string.Empty;

    /// <summary>Destination country ISO code (e.g. "US").</summary>
    [Required]
    public string DestinationCountryCode { get; init; } = string.Empty;

    /// <summary>Destination postal code for accuracy.</summary>
    public string? DestinationPostalCode { get; init; }

    /// <summary>Total package weight in kilograms.</summary>
    [Range(0.001, double.MaxValue)]
    public decimal WeightKg { get; init; }

    /// <summary>Package length in centimeters.</summary>
    public decimal? LengthCm { get; init; }

    /// <summary>Package width in centimeters.</summary>
    public decimal? WidthCm { get; init; }

    /// <summary>Package height in centimeters.</summary>
    public decimal? HeightCm { get; init; }
}

/// <summary>
/// Response containing available shipping rate options.
/// </summary>
public record ShippingRateResponseDto
{
    /// <summary>Available shipping rate options from the carrier.</summary>
    public List<ShippingRateOptionDto> Rates { get; init; } = [];
}

/// <summary>
/// A single shipping rate option from a carrier.
/// </summary>
public record ShippingRateOptionDto
{
    /// <summary>Carrier product name (e.g. "Express Worldwide").</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Total price in THB.</summary>
    public decimal TotalPrice { get; init; }

    /// <summary>Currency code for the price.</summary>
    public string CurrencyCode { get; init; } = "THB";

    /// <summary>Estimated delivery date in ISO format.</summary>
    public string? EstimatedDeliveryDate { get; init; }
}
```

- [ ] **Step 3: Build Shared project**

```
dotnet build Maliev.Intranet.Shared/Maliev.Intranet.Shared.csproj
```

Expected: Build succeeds with zero warnings.

---

### Task 2: Create DhlExpressClient (BFF proxy)

**Files:**
- Create: `Maliev.Intranet.Bff/Clients/DhlExpressClient.cs`

- [ ] **Step 1: Create DhlExpressClient.cs**

```csharp
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
    /// <param name="configuration">Application configuration for credentials.</param>
    /// <param name="logger">Logger instance.</param>
    public DhlExpressClient(HttpClient httpClient, IConfiguration configuration, ILogger<DhlExpressClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
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
```

- [ ] **Step 2: Register DhlExpressClient in BFF Program.cs**

Add in the appropriate section of `Maliev.Intranet.Bff/Program.cs` (near other HTTP client registrations, around line 444):

```csharp
// DHL Express API client (external service — no service discovery, explicit base URL)
builder.Services.AddHttpClient<DhlExpressClient>(client =>
{
    var baseUrl = builder.Configuration["DhlExpress:BaseUrl"] ?? "https://express.api.dhl.com/mydhlapi/test";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
```

- [ ] **Step 3: Build and verify**

```
dotnet build Maliev.Intranet.Bff/Maliev.Intranet.Bff.csproj
```

Expected: Build succeeds with zero warnings.

---

### Task 3: Create ShippingController (BFF endpoint)

**Files:**
- Create: `Maliev.Intranet.Bff/Controllers/ShippingController.cs`

- [ ] **Step 1: Create ShippingController.cs**

```csharp
using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for shipping rate calculation from carrier APIs.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("intranet/v{version:apiVersion}")]
public class ShippingController(DhlExpressClient dhlClient, ILogger<ShippingController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves shipping rates from DHL Express for the given package and destination.
    /// </summary>
    /// <param name="originCountry">Origin country ISO code.</param>
    /// <param name="destCountry">Destination country ISO code.</param>
    /// <param name="destPostalCode">Optional destination postal code.</param>
    /// <param name="weightKg">Package weight in kilograms.</param>
    /// <param name="lengthCm">Optional package length in cm.</param>
    /// <param name="widthCm">Optional package width in cm.</param>
    /// <param name="heightCm">Optional package height in cm.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Available shipping rates.</returns>
    [RequirePermission(MalievPermissions.Shipping.RatesRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("shipping/rates")]
    public async Task<ActionResult<MalievResponse<ShippingRateResponseDto>>> GetRates(
        [FromQuery] string originCountry,
        [FromQuery] string destCountry,
        [FromQuery] string? destPostalCode,
        [FromQuery] decimal weightKg,
        [FromQuery] decimal? lengthCm,
        [FromQuery] decimal? widthCm,
        [FromQuery] decimal? heightCm,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(originCountry) || string.IsNullOrWhiteSpace(destCountry))
        {
            return BadRequest(MalievResponse<ShippingRateResponseDto>.Error("Origin and destination countries are required."));
        }

        if (weightKg <= 0)
        {
            return BadRequest(MalievResponse<ShippingRateResponseDto>.Error("Weight must be greater than zero."));
        }

        var request = new ShippingRateRequestDto
        {
            OriginCountryCode = originCountry.Trim().ToUpperInvariant(),
            DestinationCountryCode = destCountry.Trim().ToUpperInvariant(),
            DestinationPostalCode = destPostalCode,
            WeightKg = weightKg,
            LengthCm = lengthCm,
            WidthCm = widthCm,
            HeightCm = heightCm
        };

        try
        {
            var rates = await dhlClient.GetRatesAsync(request, ct);
            return Ok(MalievResponse<ShippingRateResponseDto>.Success(rates));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "DHL rate fetch failed for {Origin} -> {Dest}", originCountry, destCountry);
            return StatusCode(502, MalievResponse<ShippingRateResponseDto>.Error("Unable to fetch shipping rates. Please try again later."));
        }
    }
}
```

- [ ] **Step 2: Build and verify**

```
dotnet build Maliev.Intranet.Bff/Maliev.Intranet.Bff.csproj
```

Expected: Build succeeds with zero warnings.

---

### Task 4: Create client-side ShippingService

**Files:**
- Create: `Maliev.Intranet.Client/Services/ShippingService.cs`
- Modify: `Maliev.Intranet.Client/Program.cs`

- [ ] **Step 1: Create ShippingService.cs**

```csharp
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
        var query = $"intranet/v1/shipping/rates" +
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
```

- [ ] **Step 2: Register ShippingService in Client Program.cs**

Add after `builder.Services.AddScoped<CurrencyService>();` (line 24):

```csharp
builder.Services.AddScoped<ShippingService>();
```

- [ ] **Step 3: Build and verify**

```
dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj
```

Expected: Build succeeds with zero warnings.

---

### Task 5: Add "Get DHL Rate" button to QuoteSummaryBar

**Files:**
- Modify: `Maliev.Intranet.Client/Components/Project/QuoteSummaryBar.razor`

- [ ] **Step 1: Add inject, parameters, and state**

In the razor directive section (after line 6, before line 7), add:

```razor
@inject ShippingService ShippingService
@inject ISnackbar Snackbar
```

In the `@code` block parameter section (after `OnGeneratePdf` parameter at line 870), add:

```csharp
    [Parameter] public string? ShippingDestinationCountry { get; set; }
    [Parameter] public string? ShippingDestinationPostalCode { get; set; }
    [Parameter] public decimal TotalWeightKg { get; set; }
```

In the `@code` block private fields section (after `_detailsOpen` at line 873), add:

```csharp
    private bool _fetchingRates;
    private List<ShippingRateOptionDto> _rateOptions = [];
```

- [ ] **Step 2: Add "Get DHL Rate" button in the Shipping section**

Replace the Shipping section (lines 155-177) with:

```razor
                    <section class="qsb-adjustment-card qsb-adjustment-card--shipping" aria-label="Shipping adjustment">
                        <div class="qsb-adjustment-top">
                            <span class="qsb-adjustment-icon">
                                <MudIcon Icon="@Icons.Material.Outlined.LocalShipping" Size="Size.Small" />
                            </span>
                            <span class="qsb-adjustment-copy">
                                <span class="qsb-adjustment-name">Shipping</span>
                                <span class="qsb-adjustment-hint">Freight, courier, or delivery charge</span>
                            </span>
                        </div>
                        <div class="qsb-money-entry">
                            <CurrencyInput Label="Shipping"
                                           HelperText="Freight, courier, or delivery charge"
                                           @bind-ThbValue="ShippingCost"
                                           Min="0m" />
                        </div>
                        @if (!string.IsNullOrWhiteSpace(ShippingDestinationCountry) && TotalWeightKg > 0)
                        {
                            <MudButton Variant="Variant.Outlined"
                                       Size="Size.Small"
                                       Color="Color.Info"
                                       StartIcon="@Icons.Material.Outlined.LocalShipping"
                                       OnClick="FetchDhlRatesAsync"
                                       Disabled="_fetchingRates"
                                       FullWidth="true">
                                @if (_fetchingRates)
                                {
                                    <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-1" />
                                }
                                Get DHL Rate
                            </MudButton>
                            @if (_rateOptions.Count > 0 && !_fetchingRates)
                            {
                                <div class="qsb-rate-options">
                                    @foreach (var rate in _rateOptions)
                                    {
                                        <button type="button"
                                                class="qsb-rate-option"
                                                @onclick="@(() => SelectDhlRate(rate))">
                                            <span class="qsb-rate-name">@rate.ProductName</span>
                                            <span class="qsb-rate-price">฿@rate.TotalPrice.ToString("N0")</span>
                                            @if (!string.IsNullOrWhiteSpace(rate.EstimatedDeliveryDate))
                                            {
                                                <span class="qsb-rate-edd">Est: @DateTime.TryParse(rate.EstimatedDeliveryDate, out var d) ? d.ToString("MMM d") : rate.EstimatedDeliveryDate</span>
                                            }
                                        </button>
                                    }
                                </div>
                            }
                        }
                    </section>
```

- [ ] **Step 3: Add handler methods in @code block**

Add after `OnQuotationTermsChanged` method (after line 911):

```csharp
    private async Task FetchDhlRatesAsync()
    {
        _fetchingRates = true;
        _rateOptions = [];
        StateHasChanged();

        try
        {
            var request = new ShippingRateRequestDto
            {
                OriginCountryCode = "TH",
                DestinationCountryCode = ShippingDestinationCountry ?? "US",
                DestinationPostalCode = ShippingDestinationPostalCode,
                WeightKg = TotalWeightKg
            };

            var result = await ShippingService.GetRatesAsync(request);
            if (result?.Rates is { Count: > 0 })
            {
                _rateOptions = result.Rates;
            }
            else
            {
                Snackbar.Add("No shipping rates available for this destination.", Severity.Warning);
            }
        }
        catch (Exception)
        {
            Snackbar.Add("Failed to fetch shipping rates. Please try again.", Severity.Error);
        }
        finally
        {
            _fetchingRates = false;
            StateHasChanged();
        }
    }

    private async Task SelectDhlRate(ShippingRateOptionDto rate)
    {
        await ShippingCostChanged.InvokeAsync(rate.TotalPrice);
        _rateOptions = [];
        StateHasChanged();
    }
```

- [ ] **Step 4: Add CSS for rate options**

Add before the closing `</style>` tag (before line 850):

```css
    .qsb-rate-options {
        display: flex;
        flex-direction: column;
        gap: 4px;
    }

    .qsb-rate-option {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 6px;
        padding: 6px 8px;
        border: 1px solid color-mix(in oklch, var(--mud-palette-info) 24%, var(--maliev-border));
        border-radius: 6px;
        background: color-mix(in oklch, var(--mud-palette-info) 5%, var(--maliev-panel));
        cursor: pointer;
        font-family: inherit;
        text-align: left;
        transition: background 0.15s;
        min-width: 0;
    }

    .qsb-rate-option:hover {
        background: color-mix(in oklch, var(--mud-palette-info) 10%, var(--maliev-panel));
    }

    .qsb-rate-name {
        flex: 0 0 auto;
        font-size: 11px;
        font-weight: 600;
        color: var(--maliev-ink);
    }

    .qsb-rate-price {
        font-family: 'JetBrains Mono', monospace;
        font-size: 12px;
        font-weight: 700;
        color: var(--mud-palette-primary);
    }

    .qsb-rate-edd {
        font-size: 10px;
        color: var(--maliev-muted);
        margin-left: auto;
    }
```

- [ ] **Step 5: Build entire solution**

```
dotnet build Maliev.Intranet.slnx
```

Expected: Build succeeds with zero warnings.

- [ ] **Step 6: Run all tests**

```
dotnet test Maliev.Intranet.slnx --verbosity normal
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```
git add Maliev.Intranet.Shared/Dtos/ShippingDtos.cs Maliev.Intranet.Shared/Dtos/MalievPermissions.cs Maliev.Intranet.Bff/Clients/DhlExpressClient.cs Maliev.Intranet.Bff/Controllers/ShippingController.cs Maliev.Intranet.Bff/Program.cs Maliev.Intranet.Client/Services/ShippingService.cs Maliev.Intranet.Client/Program.cs Maliev.Intranet.Client/Components/Project/QuoteSummaryBar.razor
git commit -m "feat: add DHL Express rate integration via BFF proxy"
```
