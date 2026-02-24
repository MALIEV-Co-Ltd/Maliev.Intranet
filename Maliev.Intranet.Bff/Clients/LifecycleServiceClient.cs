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
        try
        {
            var response = await httpClient.GetAsync($"/lifecycle/v1/onboarding/pending?page={page}&pageSize={pageSize}", ct);
            if (!response.IsSuccessStatusCode) return new PagedResponse<OnboardingSummaryDto>();

            // Lifecycle service returns IEnumerable<OnboardingStatusDto> (raw array), not a paged wrapper.
            // Deserialize as JsonElement array and map to OnboardingSummaryDto.
            var items = await response.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>(cancellationToken: ct) ?? [];

            var mapped = items.Select(item =>
            {
                var totalItems = item.TryGetProperty("totalItems", out var ti) ? ti.GetInt32() : 0;
                var completedItems = item.TryGetProperty("completedItems", out var ci) ? ci.GetInt32() : 0;
                return new OnboardingSummaryDto
                {
                    Id = item.TryGetProperty("id", out var id) ? id.GetGuid() : Guid.Empty,
                    EmployeeId = item.TryGetProperty("employeeId", out var empId) ? empId.GetGuid() : Guid.Empty,
                    EmployeeName = string.Empty,
                    Department = string.Empty,
                    StartDate = item.TryGetProperty("startDate", out var sd) && sd.TryGetDateTime(out var startDate) ? startDate : DateTime.MinValue,
                    Progress = totalItems > 0 ? completedItems * 100 / totalItems : 0,
                    Buddy = string.Empty,
                    Status = item.TryGetProperty("status", out var status) ? status.GetString() ?? string.Empty : string.Empty
                };
            }).ToList();

            return new PagedResponse<OnboardingSummaryDto>
            {
                Data = mapped,
                Meta = new PaginationMeta
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = mapped.Count,
                    TotalCount = mapped.Count,
                    TotalPages = 1
                }
            };
        }
        catch
        {
            return new PagedResponse<OnboardingSummaryDto>();
        }
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
