using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for reference data endpoints (countries, currencies, etc.).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferenceDataController(
    IReferenceDataService referenceDataService,
    CurrencyServiceClient currencyClient,
    ILogger<ReferenceDataController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all countries.
    /// </summary>
    [HttpGet("countries")]
    public async Task<ActionResult<List<CountryDto>>> GetCountries(CancellationToken cancellationToken)
    {
        try
        {
            var countries = await referenceDataService.GetCountriesAsync(cancellationToken);
            return Ok(countries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching countries");
            return StatusCode(500, "An error occurred while fetching countries");
        }
    }

    /// <summary>
    /// Gets all active currencies.
    /// </summary>
    [HttpGet("currencies")]
    public async Task<ActionResult<List<CurrencyDto>>> GetCurrencies(CancellationToken cancellationToken)
    {
        try
        {
            var result = await currencyClient.GetCurrenciesAsync(pageSize: 200, ct: cancellationToken);
            return Ok(result?.Data ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching currencies");
            return StatusCode(500, "An error occurred while fetching currencies");
        }
    }

    /// <summary>
    /// Creates a new currency.
    /// </summary>
    [HttpPost("currencies")]
    public async Task<ActionResult<CurrencyDto>> CreateCurrency([FromBody] CreateCurrencyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await currencyClient.CreateCurrencyAsync(request, cancellationToken);
            return result != null ? Ok(result) : BadRequest(new ApiErrorResponse { Message = "Failed to create currency." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating currency");
            return StatusCode(500, new ApiErrorResponse { Message = "An error occurred while creating the currency." });
        }
    }
}
