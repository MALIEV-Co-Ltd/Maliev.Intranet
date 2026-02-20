using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Lifecycle (Onboarding) microservice.
/// </summary>
public interface ILifecycleServiceClient
{
    /// <summary>
    /// Retrieves a paged list of onboarding summaries.
    /// </summary>
    Task<PagedResponse<OnboardingSummaryDto>?> GetOnboardingsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>
    /// Retrieves an onboarding checklist for an employee.
    /// </summary>
    Task<OnboardingChecklistDto?> GetChecklistAsync(Guid employeeId, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of an onboarding task.
    /// </summary>
    Task<bool> UpdateTaskAsync(Guid employeeId, UpdateOnboardingProgressRequest request, CancellationToken ct = default);
}

/// <summary>
/// Default implementation of the lifecycle service client.
/// </summary>
public class LifecycleServiceClient(HttpClient httpClient) : ILifecycleServiceClient
{
    /// <inheritdoc />
    public async Task<PagedResponse<OnboardingSummaryDto>?> GetOnboardingsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<OnboardingSummaryDto>>($"/lifecycle/v1/onboarding?page={page}&pageSize={pageSize}", ct);
    }

    /// <inheritdoc />
    public async Task<OnboardingChecklistDto?> GetChecklistAsync(Guid employeeId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<OnboardingChecklistDto>($"/lifecycle/v1/onboarding/{employeeId}/checklist", ct);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateTaskAsync(Guid employeeId, UpdateOnboardingProgressRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/lifecycle/v1/onboarding/{employeeId}/tasks/{request.TaskId}", request, ct);
        return response.IsSuccessStatusCode;
    }
}
