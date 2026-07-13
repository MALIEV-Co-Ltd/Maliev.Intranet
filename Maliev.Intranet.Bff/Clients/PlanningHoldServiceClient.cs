using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Performs production-planning hold operations using the BFF service identity.
/// Employee authorization is evaluated separately against the authoritative project resource.
/// </summary>
public sealed class PlanningHoldServiceClient(HttpClient httpClient)
{
    private const string DelegatedActorHeader = "X-Maliev-Delegated-Actor-Id";

    /// <summary>Gets a planning hold by its immutable identifier.</summary>
    public async Task<HttpResponseMessage> GetAsync(Guid holdId, CancellationToken ct = default) =>
        await httpClient.GetAsync($"/job/v1/jobs/planning-holds/{holdId:D}", ct);

    /// <summary>Creates a planning hold after the BFF has authorized the owning project.</summary>
    public async Task<HttpResponseMessage> CreateAsync(
        CreateProductionPlanningHoldRequest request,
        string delegatedActorId,
        CancellationToken ct = default) =>
        await SendCreateAsync(request, delegatedActorId, ct);

    private async Task<HttpResponseMessage> SendCreateAsync(
        CreateProductionPlanningHoldRequest request,
        string delegatedActorId,
        CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/job/v1/jobs/planning-holds")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add(DelegatedActorHeader, delegatedActorId);
        return await httpClient.SendAsync(message, ct);
    }

    /// <summary>Updates a planning hold after the BFF has resolved and authorized its owning project.</summary>
    public async Task<HttpResponseMessage> UpdateAsync(
        Guid holdId,
        UpdateProductionPlanningHoldRequest request,
        CancellationToken ct = default) =>
        await httpClient.PatchAsJsonAsync($"/job/v1/jobs/planning-holds/{holdId:D}", request, ct);

    /// <summary>Cancels a planning hold after the BFF has resolved and authorized its owning project.</summary>
    public async Task<HttpResponseMessage> CancelAsync(Guid holdId, CancellationToken ct = default) =>
        await httpClient.DeleteAsync($"/job/v1/jobs/planning-holds/{holdId:D}", ct);
}
