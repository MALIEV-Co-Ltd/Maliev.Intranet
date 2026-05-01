using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for pricing features including snapshots and AI-driven calculations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/pricing")]
public class PricingController(IPricingServiceClient pricingClient, MaterialServiceClient materialClient) : ControllerBase
{
    /// <summary>
    /// Lists snapshots for an order.
    /// </summary>
    [RequirePermission(MalievPermissions.Pricing.ReadConfig, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("snapshots")]
    public async Task<ActionResult<List<PricingSnapshotDto>>> GetSnapshots(string orderId, CancellationToken ct)
    {
        var result = await pricingClient.GetSnapshotsByOrderAsync(orderId, ct);
        return Ok(result ?? []);
    }

    /// <summary>
    /// Gets a pricing snapshot by ID.
    /// </summary>
    [RequirePermission(MalievPermissions.Pricing.ReadConfig, AuthenticationSchemes = "Bearer,Cookies")]
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
    [RequirePermission(MalievPermissions.Pricing.WriteConfig, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("snapshots")]
    public async Task<ActionResult<PricingSnapshotDto>> CreateSnapshot(CreatePricingSnapshotRequest request, CancellationToken ct)
    {
        var result = await pricingClient.CreateSnapshotAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Accepts a snapshot.
    /// </summary>
    [RequirePermission(MalievPermissions.Pricing.WriteConfig, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("snapshots/{id:guid}/accept")]
    public async Task<ActionResult<PricingSnapshotDto>> AcceptSnapshot(Guid id, CancellationToken ct)
    {
        var result = await pricingClient.AcceptSnapshotAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Calculates the price for a given set of inputs.
    /// When a finish is selected, the finish surcharge is added on top of the base price.
    /// </summary>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("calculate")]
    public async Task<ActionResult<PricingResultDto>> CalculatePrice(PricingRequestDto request, CancellationToken ct)
    {
        var result = await pricingClient.CalculatePriceAsync(request, ct);

        // Apply surface finish surcharge when a finish is selected.
        if (request.FinishId.HasValue && result != null)
        {
            var processCode = request.ManufacturingProcessCode;
            if (!string.IsNullOrEmpty(processCode))
            {
                var finishes = await materialClient.GetFinishesByProcessAsync(processCode, ct);
                var finish = finishes?.FirstOrDefault(f => f.Id == request.FinishId.Value);
                if (finish != null)
                {
                    var surcharge = Math.Round(result.TotalUnitPrice * finish.AdditionalCostPercent, 2);
                    result = result with
                    {
                        TotalUnitPrice = result.TotalUnitPrice + surcharge,
                        TotalPrice = (result.TotalUnitPrice + surcharge) * request.Quantity,
                    };
                }
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Returns all active lead time options with price multipliers.
    /// </summary>
    [RequirePermission(MalievPermissions.Pricing.ReadConfig, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("lead-times")]
    public async Task<ActionResult<List<LeadTimeOptionDto>>> GetLeadTimes(CancellationToken ct)
    {
        var result = await pricingClient.GetLeadTimeOptionsAsync(ct);
        return Ok((result ?? []).Where(lt => !lt.Code.Equals("RUSH", StringComparison.OrdinalIgnoreCase)).ToList());
    }

    /// <summary>
    /// Calculates bulk pricing for multiple quantities given a base unit price.
    /// </summary>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("bulk")]
    public async Task<ActionResult<List<BulkPriceTierDto>>> GetBulkPricing(BulkPricingRequestDto request, CancellationToken ct)
    {
        var result = await pricingClient.GetBulkPricingAsync(request, ct);
        return Ok(result ?? []);
    }

    /// <summary>
    /// Returns the absolute additional unit cost for each requested surface finish,
    /// computed from the catalog's cost-percent multiplied by the caller-supplied base price.
    /// The base price should reflect the process + material + tolerance combination already
    /// factored in by the pricing engine.
    /// </summary>
    [RequirePermission(MalievPermissions.Pricing.ReadConfig, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("finish-options")]
    public async Task<ActionResult<List<FinishPriceItemDto>>> GetFinishPrices(
        FinishPriceRequestDto request, CancellationToken ct)
    {
        var finishes = await materialClient.GetFinishesByProcessAsync(request.ProcessCode, ct);
        if (finishes == null || finishes.Count == 0)
            return Ok(Array.Empty<FinishPriceItemDto>());

        var finishIdSet = request.FinishIds.ToHashSet();
        var result = finishes
            .Where(f => finishIdSet.Contains(f.Id))
            .Select(f => new FinishPriceItemDto(
                f.Id,
                Math.Round(request.BaseUnitPrice * f.AdditionalCostPercent, 2),
                "THB"))
            .ToList();

        return Ok(result);
    }
}
