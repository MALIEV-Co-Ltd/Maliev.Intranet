using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Interface for interacting with the Pricing microservice.
/// </summary>
public interface IPricingServiceClient
{
    /// <summary>
    /// Creates a pricing snapshot.
    /// </summary>
    Task<PricingSnapshotDto?> CreateSnapshotAsync(CreatePricingSnapshotRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a pricing snapshot by ID.
    /// </summary>
    Task<PricingSnapshotDto?> GetSnapshotAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lists snapshots for an order.
    /// </summary>
    Task<List<PricingSnapshotDto>?> GetSnapshotsByOrderAsync(string orderId, CancellationToken ct = default);

    /// <summary>
    /// Accepts a pricing snapshot.
    /// </summary>
    Task<PricingSnapshotDto?> AcceptSnapshotAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Calculates the price for a given set of inputs.
    /// </summary>
    Task<PricingResultDto?> CalculatePriceAsync(PricingRequestDto request, CancellationToken ct = default);

    /// <summary>Returns all active lead time options.</summary>
    Task<List<LeadTimeOptionDto>?> GetLeadTimeOptionsAsync(CancellationToken ct = default);

    /// <summary>Calculates bulk pricing for a range of quantities.</summary>
    Task<List<BulkPriceTierDto>?> GetBulkPricingAsync(BulkPricingRequestDto request, CancellationToken ct = default);
}
