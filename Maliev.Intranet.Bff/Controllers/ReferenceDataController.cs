using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for reference data endpoints (countries, etc.).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ReferenceDataController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;
    private readonly CurrencyServiceClient _currencyClient;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ReferenceDataController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataController"/> class.
    /// </summary>
    /// <param name="referenceDataService">Reference data service.</param>
    /// <param name="currencyClient">Currency service client.</param>
    /// <param name="environment">The current hosting environment.</param>
    /// <param name="logger">Logger instance.</param>
    public ReferenceDataController(
        IReferenceDataService referenceDataService,
        CurrencyServiceClient currencyClient,
        IWebHostEnvironment environment,
        ILogger<ReferenceDataController> logger)
    {
        _referenceDataService = referenceDataService;
        _currencyClient = currencyClient;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Gets all countries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of countries.</returns>
    [RequirePermission(MalievPermissions.Registry.LocationsRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("countries")]
    public async Task<ActionResult<List<CountryDto>>> GetCountries(CancellationToken cancellationToken)
    {
        try
        {
            var countries = await _referenceDataService.GetCountriesAsync(cancellationToken);
            return Ok(countries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching countries");
            return Problem(
                title: "Country reference data unavailable",
                detail: _environment.IsProduction() ? null : ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets a paginated country page for reference data management.
    /// </summary>
    [RequirePermission(MalievPermissions.Country.CountriesList, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("countries/page")]
    public async Task<ActionResult<ReferenceDataPage<CountryDto>>> GetCountryPage(
        [FromQuery] string? query,
        [FromQuery] bool includeInactive = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await _referenceDataService.GetCountryPageAsync(query, includeInactive, pageNumber, pageSize, cancellationToken);
            return Ok(page);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching country page");
            return Problem(title: "Country reference data unavailable", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Creates a country reference record.
    /// </summary>
    [RequirePermission(MalievPermissions.Country.CountriesCreate, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("countries")]
    public async Task<ActionResult<CountryDto>> CreateCountry([FromBody] CountryDto request, CancellationToken cancellationToken)
    {
        var country = await _referenceDataService.CreateCountryAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCountryPage), new { id = country.Id }, country);
    }

    /// <summary>
    /// Updates a country reference record.
    /// </summary>
    [RequirePermission(MalievPermissions.Country.CountriesUpdate, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("countries/{id:guid}")]
    public async Task<ActionResult<CountryDto>> UpdateCountry(Guid id, [FromBody] CountryDto request, CancellationToken cancellationToken)
    {
        var country = await _referenceDataService.UpdateCountryAsync(id, request, cancellationToken);
        return Ok(country);
    }

    /// <summary>
    /// Soft deletes a country reference record.
    /// </summary>
    [RequirePermission(MalievPermissions.Country.CountriesDelete, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("countries/{id:guid}")]
    public async Task<IActionResult> DeleteCountry(Guid id, CancellationToken cancellationToken)
    {
        await _referenceDataService.DeleteCountryAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Restores a soft-deleted country reference record.
    /// </summary>
    [RequirePermission(MalievPermissions.Country.CountriesRestore, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("countries/{id:guid}/restore")]
    public async Task<IActionResult> RestoreCountry(Guid id, CancellationToken cancellationToken)
    {
        await _referenceDataService.RestoreCountryAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets all currencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of currencies.</returns>
    [RequirePermission(MalievPermissions.Currency.CurrenciesRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("currencies")]
    public async Task<ActionResult<List<CurrencyDto>>> GetCurrencies(CancellationToken cancellationToken)
    {
        try
        {
            var currencies = await _referenceDataService.GetCurrenciesAsync(cancellationToken);
            return Ok(currencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currencies");
            return StatusCode(500, "An error occurred while fetching currencies");
        }
    }

    /// <summary>
    /// Gets a paginated currency page for reference data management.
    /// </summary>
    [RequirePermission(MalievPermissions.Currency.CurrenciesRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("currencies/page")]
    public async Task<ActionResult<ReferenceDataPage<CurrencyDto>>> GetCurrencyPage(
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var page = await _referenceDataService.GetCurrencyPageAsync(isActive, pageNumber, pageSize, cancellationToken);
        return Ok(page);
    }

    /// <summary>
    /// Creates a currency reference record.
    /// </summary>
    [RequirePermission(MalievPermissions.Currency.CurrenciesCreate, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("currencies")]
    public async Task<ActionResult<CurrencyDto>> CreateCurrency([FromBody] CurrencyDto request, CancellationToken cancellationToken)
    {
        var currency = await _referenceDataService.CreateCurrencyAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCurrencyPage), new { id = currency.Id }, currency);
    }

    /// <summary>
    /// Updates a currency reference record.
    /// </summary>
    [RequirePermission(MalievPermissions.Currency.CurrenciesUpdate, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("currencies/{id:guid}")]
    public async Task<ActionResult<CurrencyDto>> UpdateCurrency(Guid id, [FromBody] CurrencyDto request, CancellationToken cancellationToken)
    {
        var currency = await _referenceDataService.UpdateCurrencyAsync(id, request, cancellationToken);
        return Ok(currency);
    }

    /// <summary>
    /// Deletes a currency reference record.
    /// </summary>
    [RequirePermission(MalievPermissions.Currency.CurrenciesDelete, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("currencies/{id:guid}")]
    public async Task<IActionResult> DeleteCurrency(Guid id, CancellationToken cancellationToken)
    {
        await _referenceDataService.DeleteCurrencyAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets the primary currency.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The primary currency.</returns>
    [RequirePermission(MalievPermissions.Currency.CurrenciesRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("currencies/primary")]
    public async Task<ActionResult<CurrencyDto>> GetPrimaryCurrency(CancellationToken cancellationToken)
    {
        try
        {
            var currency = await _referenceDataService.GetPrimaryCurrencyAsync(cancellationToken);
            if (currency == null)
            {
                return NotFound("No primary currency configured");
            }
            return Ok(currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching primary currency");
            return StatusCode(500, "An error occurred while fetching primary currency");
        }
    }

    /// <summary>
    /// Gets the live exchange rate between two currencies.
    /// Returns 1.0 when from == to.
    /// </summary>
    /// <param name="from">Source currency code (ISO 4217).</param>
    /// <param name="to">Target currency code (ISO 4217).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exchange rate as a decimal.</returns>
    [RequirePermission(MalievPermissions.Currency.RatesRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("currencies/rate")]
    public async Task<ActionResult<ExchangeRateResponse>> GetExchangeRate(
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return BadRequest("Both 'from' and 'to' currency codes are required.");

        var rate = await _currencyClient.GetExchangeRateAsync(from, to, cancellationToken);
        if (rate == null)
            return StatusCode(503, "Exchange rate temporarily unavailable.");

        return Ok(new ExchangeRateResponse(from.ToUpperInvariant(), to.ToUpperInvariant(), rate.Value));
    }

    /// <summary>
    /// Gets a paginated Thai registry location page.
    /// </summary>
    [RequirePermission(MalievPermissions.Registry.LocationsRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("locations")]
    public async Task<ActionResult<ReferenceDataPage<RegistryThaiLocation>>> GetRegistryLocations(
        [FromQuery] string? query,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var page = await _referenceDataService.GetRegistryLocationPageAsync(query, pageNumber, pageSize, cancellationToken);
        return Ok(page);
    }

    /// <summary>
    /// Creates a Thai registry location.
    /// </summary>
    [RequirePermission(MalievPermissions.Registry.LocationsCreate, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("locations")]
    public async Task<ActionResult<RegistryThaiLocation>> CreateRegistryLocation(
        [FromBody] RegistryThaiLocation request,
        CancellationToken cancellationToken)
    {
        var location = await _referenceDataService.CreateRegistryLocationAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRegistryLocations), new { id = location.Id }, location);
    }

    /// <summary>
    /// Updates a Thai registry location.
    /// </summary>
    [RequirePermission(MalievPermissions.Registry.LocationsUpdate, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("locations/{id:guid}")]
    public async Task<ActionResult<RegistryThaiLocation>> UpdateRegistryLocation(
        Guid id,
        [FromBody] RegistryThaiLocation request,
        CancellationToken cancellationToken)
    {
        var location = await _referenceDataService.UpdateRegistryLocationAsync(id, request, cancellationToken);
        return Ok(location);
    }

    /// <summary>
    /// Deletes a Thai registry location.
    /// </summary>
    [RequirePermission(MalievPermissions.Registry.LocationsDelete, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("locations/{id:guid}")]
    public async Task<IActionResult> DeleteRegistryLocation(Guid id, CancellationToken cancellationToken)
    {
        await _referenceDataService.DeleteRegistryLocationAsync(id, cancellationToken);
        return NoContent();
    }
}
