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
    public async Task<PricingSnapshotDto?> CreateSnapshotAsync(CreatePricingSnapshotRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/pricing/v1/snapshots", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PricingSnapshotDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Gets a pricing snapshot by ID.
    /// </summary>
    public async Task<PricingSnapshotDto?> GetSnapshotAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PricingSnapshotDto>($"/pricing/v1/snapshots/{id}", ct);
    }

    /// <summary>
    /// Lists snapshots for an order.
    /// </summary>
    public async Task<List<PricingSnapshotDto>?> GetSnapshotsByOrderAsync(string orderId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<PricingSnapshotDto>>($"/pricing/v1/snapshots?orderId={orderId}", ct);
    }

    /// <summary>
    /// Accepts a pricing snapshot.
    /// </summary>
    public async Task<PricingSnapshotDto?> AcceptSnapshotAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsync($"/pricing/v1/snapshots/{id}/accept", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PricingSnapshotDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Calculates the price for a given set of inputs.
    /// </summary>
    public async Task<PricingResultDto?> CalculatePriceAsync(PricingRequestDto request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/pricing/v1/pricing/calculate", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PricingResultDto>(cancellationToken: ct);
    }
}
