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
[Route("api/v{version:apiVersion}/shipping")]
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
    [HttpGet("rates")]
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
            return BadRequest(new MalievResponse<ShippingRateResponseDto>
            {
                Success = false,
                Message = "Origin and destination countries are required."
            });
        }

        if (weightKg <= 0)
        {
            return BadRequest(new MalievResponse<ShippingRateResponseDto>
            {
                Success = false,
                Message = "Weight must be greater than zero."
            });
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
            return Ok(new MalievResponse<ShippingRateResponseDto>
            {
                Success = true,
                Data = rates
            });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "DHL rate fetch failed for {Origin} -> {Dest}", originCountry, destCountry);
            return StatusCode(502, new MalievResponse<ShippingRateResponseDto>
            {
                Success = false,
                Message = "Unable to fetch shipping rates. Please try again later."
            });
        }
    }
}
