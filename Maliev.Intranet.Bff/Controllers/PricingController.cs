using Asp.Versioning;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for pricing features including snapshots and AI-driven calculations.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/pricing")]
public class PricingController(IPricingServiceClient pricingClient) : ControllerBase
{
    /// <summary>
    /// Lists snapshots for an order.
    /// </summary>
    [HttpGet("snapshots")]
    public async Task<ActionResult<List<PricingSnapshotDto>>> GetSnapshots(string orderId, CancellationToken ct)
    {
        var result = await pricingClient.GetSnapshotsByOrderAsync(orderId, ct);
        return Ok(result ?? []);
    }

    /// <summary>
    /// Gets a pricing snapshot by ID.
    /// </summary>
    [HttpGet("snapshots/{id:guid}")]
    public async Task<ActionResult<PricingSnapshotDto>> GetSnapshot(Guid id, CancellationToken ct)
    {
        var result = await pricingClient.GetSnapshotAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Creates a pricing snapshot.
    /// </summary>
    [HttpPost("snapshots")]
    public async Task<ActionResult<PricingSnapshotDto>> CreateSnapshot(CreatePricingSnapshotRequest request, CancellationToken ct)
    {
        var result = await pricingClient.CreateSnapshotAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Accepts a snapshot.
    /// </summary>
    [HttpPatch("snapshots/{id:guid}/accept")]
    public async Task<ActionResult<PricingSnapshotDto>> AcceptSnapshot(Guid id, CancellationToken ct)
    {
        var result = await pricingClient.AcceptSnapshotAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Calculates the price for a given set of inputs.
    /// </summary>
    [HttpPost("calculate")]
    public async Task<ActionResult<PricingResultDto>> CalculatePrice(PricingRequestDto request, CancellationToken ct)
    {
        var result = await pricingClient.CalculatePriceAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns all active lead time options with price multipliers.
    /// </summary>
    [HttpGet("lead-times")]
    public async Task<ActionResult<List<LeadTimeOptionDto>>> GetLeadTimes(CancellationToken ct)
    {
        var result = await pricingClient.GetLeadTimeOptionsAsync(ct);
        return Ok((result ?? []).Where(lt => !lt.Code.Equals("RUSH", StringComparison.OrdinalIgnoreCase)).ToList());
    }

    /// <summary>
    /// Calculates bulk pricing for multiple quantities given a base unit price.
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult<List<BulkPriceTierDto>>> GetBulkPricing(BulkPricingRequestDto request, CancellationToken ct)
    {
        var result = await pricingClient.GetBulkPricingAsync(request, ct);
        return Ok(result ?? []);
    }
}
