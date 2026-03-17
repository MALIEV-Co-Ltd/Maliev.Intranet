using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for reference data endpoints (countries, etc.).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferenceDataController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;
    private readonly ILogger<ReferenceDataController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataController"/> class.
    /// </summary>
    /// <param name="referenceDataService">Reference data service.</param>
    /// <param name="logger">Logger instance.</param>
    public ReferenceDataController(IReferenceDataService referenceDataService, ILogger<ReferenceDataController> logger)
    {
        _referenceDataService = referenceDataService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all countries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of countries.</returns>
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
            return StatusCode(500, "An error occurred while fetching countries");
        }
    }

    /// <summary>
    /// Gets all currencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of currencies.</returns>
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
}
