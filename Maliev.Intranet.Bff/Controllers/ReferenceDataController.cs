using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
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
    ICurrencyServiceClient currencyClient,
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
    /// Updates a country.
    /// </summary>
    [HttpPatch("countries/{code}")]
    public async Task<ActionResult<CountryDto>> UpdateCountry(string code, [FromBody] UpdateCountryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await referenceDataService.UpdateCountryAsync(code, request.Name, cancellationToken);
            return result != null ? Ok(result) : NotFound(new ApiErrorResponse { Message = "Country not found." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating country {Code}", code);
            return StatusCode(500, new ApiErrorResponse { Message = "An error occurred while updating the country." });
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

    /// <summary>
    /// Updates a currency.
    /// </summary>
    [HttpPut("currencies/{id:guid}")]
    public async Task<ActionResult<CurrencyDto>> UpdateCurrency(Guid id, [FromBody] UpdateCurrencyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await currencyClient.UpdateCurrencyAsync(id, request, cancellationToken);
            return result != null ? Ok(result) : NotFound(new ApiErrorResponse { Message = "Currency not found." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating currency {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "An error occurred while updating the currency." });
        }
    }

    /// <summary>
    /// Deletes a currency.
    /// </summary>
    [HttpDelete("currencies/{id:guid}")]
    public async Task<ActionResult> DeleteCurrency(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await currencyClient.DeleteCurrencyAsync(id, cancellationToken);
            return result ? Ok() : NotFound(new ApiErrorResponse { Message = "Currency not found." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting currency {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "An error occurred while deleting the currency." });
        }
    }
}

/// <summary>
/// Request to update a country.
/// </summary>
public class UpdateCountryRequest
{
    /// <summary>Gets or sets the country name.</summary>
    public string Name { get; set; } = string.Empty;
}
