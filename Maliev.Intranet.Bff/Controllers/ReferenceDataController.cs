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
}
