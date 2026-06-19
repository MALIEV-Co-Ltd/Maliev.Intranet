using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for shipping courier, rate, and tracking data from DeliveryService.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shipping")]
public class ShippingController(IDeliveryServiceClient deliveryServiceClient, ILogger<ShippingController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves available shipping couriers.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Available couriers.</returns>
    [RequirePermission(MalievPermissions.Shipping.RatesRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("couriers")]
    public async Task<ActionResult<MalievResponse<List<ShippingCourierDto>>>> GetCouriers(CancellationToken ct)
    {
        var couriers = await deliveryServiceClient.GetShippingCouriersAsync(ct);
        return Ok(new MalievResponse<List<ShippingCourierDto>> { Success = true, Data = couriers });
    }

    /// <summary>
    /// Retrieves shipping rates for a shipment.
    /// </summary>
    /// <param name="request">Shipment rate request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Available shipping rates.</returns>
    [RequirePermission(MalievPermissions.Shipping.RatesRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("rates")]
    public async Task<ActionResult<MalievResponse<ShippingRateResponseDto>>> GetRates(
        [FromBody] ShippingRateRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var rates = await deliveryServiceClient.GetShippingRatesAsync(request, ct);
            return Ok(new MalievResponse<ShippingRateResponseDto>
            {
                Success = true,
                Data = rates
            });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "DeliveryService shipping rate fetch failed");
            return StatusCode(502, new MalievResponse<ShippingRateResponseDto>
            {
                Success = false,
                Message = "Unable to fetch shipping rates. Please try again later."
            });
        }
    }

    /// <summary>
    /// Retrieves tracking status for a shipment.
    /// </summary>
    /// <param name="trackingCode">Tracking code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Current tracking status.</returns>
    [RequirePermission(MalievPermissions.Shipping.RatesRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("tracking/{trackingCode}")]
    public async Task<ActionResult<MalievResponse<ShippingTrackingDto>>> GetTracking(
        [FromRoute] string trackingCode,
        CancellationToken ct)
    {
        var tracking = await deliveryServiceClient.GetShippingTrackingAsync(trackingCode, ct);
        if (tracking is null)
        {
            return NotFound(new MalievResponse<ShippingTrackingDto>
            {
                Success = false,
                Message = "Tracking status was not found."
            });
        }

        return Ok(new MalievResponse<ShippingTrackingDto> { Success = true, Data = tracking });
    }
}
