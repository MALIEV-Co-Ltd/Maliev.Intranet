using System.Text.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Pricing microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class PricingServiceClient(HttpClient httpClient) : IPricingServiceClient
{
    /// <summary>
    /// Creates a pricing snapshot.
    /// </summary>
    public Task<PricingSnapshotDto?> CreateSnapshotAsync(CreatePricingSnapshotRequest request, CancellationToken ct = default)
    {
        return Task.FromResult<PricingSnapshotDto?>(null);
    }

    /// <summary>
    /// Gets a pricing snapshot by ID.
    /// </summary>
    public Task<PricingSnapshotDto?> GetSnapshotAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult<PricingSnapshotDto?>(null);
    }

    /// <summary>
    /// Lists snapshots for an order.
    /// </summary>
    public Task<List<PricingSnapshotDto>?> GetSnapshotsByOrderAsync(string orderId, CancellationToken ct = default)
    {
        return Task.FromResult<List<PricingSnapshotDto>?>([]);
    }

    /// <summary>
    /// Accepts a pricing snapshot.
    /// </summary>
    public Task<PricingSnapshotDto?> AcceptSnapshotAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult<PricingSnapshotDto?>(null);
    }

    /// <summary>
    /// Calculates the price for a given set of inputs.
    /// </summary>
    public async Task<PricingResultDto?> CalculatePriceAsync(PricingRequestDto request, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            var downstreamRequest = request with
            {
                ManufacturingProcessName = string.IsNullOrWhiteSpace(request.ManufacturingProcessCode)
                    ? request.ManufacturingProcessName
                    : request.ManufacturingProcessCode
            };

            var apiResponse = await httpClient.PostAsJsonAsync("/pricing/v1/calculate", downstreamRequest, cts.Token);
            apiResponse.EnsureSuccessStatusCode();

            // Deserialize to the actual PricingService response first
            var serviceResult = await apiResponse.Content.ReadFromJsonAsync<PricingServiceCalculateResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cts.Token);

            if (serviceResult == null)
                return null;

            // Map to the detailed DTO expected by the Intranet
            return new PricingResultDto
            {
                Strategy = PricingStrategy.RuleBased,
                MaterialCost = 0, // Not provided by current PricingService
                SupportMaterialCost = 0,
                MachineTimeCost = 0,
                SetupCost = 0,
                ComplexitySurcharge = 0,
                SubtotalBeforeMargin = serviceResult.UnitPrice,
                MarginAmount = 0,
                TotalUnitPrice = serviceResult.UnitPrice,
                UnitPriceBeforeFinish = serviceResult.UnitPrice,
                UnitPriceBeforeVolumeDiscount = serviceResult.UnitPriceBeforeVolumeDiscount > 0m
                    ? serviceResult.UnitPriceBeforeVolumeDiscount
                    : serviceResult.UnitPrice,
                VolumeDiscountUnitAmount = serviceResult.VolumeDiscountUnitAmount,
                VolumeDiscountPercent = serviceResult.VolumeDiscountPercent,
                TotalPrice = serviceResult.TotalAmount,
                ConfidenceLevel = serviceResult.ConfidenceScore,
                ValidUntil = DateTime.UtcNow.AddHours(24),
                CalculationDuration = TimeSpan.Zero,
                EstimatedLeadTimeDays = serviceResult.EstimatedLeadTimeDays > 0 ? serviceResult.EstimatedLeadTimeDays : null,
            };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timeout occurred but original cancellation was not requested
            return null;
        }
    }

    /// <summary>Returns all active lead time options.</summary>
    public Task<List<LeadTimeOptionDto>?> GetLeadTimeOptionsAsync(CancellationToken ct = default) =>
        httpClient.GetFromJsonAsync<List<LeadTimeOptionDto>>("/pricing/v1/catalog/lead-times", ct);

    /// <summary>Calculates bulk pricing for a range of quantities.</summary>
    public async Task<List<BulkPriceTierDto>?> GetBulkPricingAsync(BulkPricingRequestDto request, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var response = await httpClient.PostAsJsonAsync("/pricing/v1/catalog/bulk-pricing", request, cts.Token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<BulkPriceTierDto>>(cancellationToken: cts.Token);
    }
}
