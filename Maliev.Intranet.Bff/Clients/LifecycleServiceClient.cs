using System.Text.Json;
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
        var offset = Math.Max(page - 1, 0) * pageSize;
        var response = await httpClient.GetFromJsonAsync<List<LifecycleOnboardingStatusResponse>>($"/lifecycle/v1/onboarding/pending?offset={offset}&limit={pageSize}", ct) ?? [];

        return new PagedResponse<OnboardingSummaryDto>
        {
            Data = response.Select(ToOnboardingSummary).ToList(),
            Meta = new PaginationMeta
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = response.Count,
                TotalItems = response.Count,
                TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(response.Count / (double)pageSize)
            }
        };
    }

    /// <inheritdoc />
    public async Task<OnboardingChecklistDto?> GetChecklistAsync(Guid employeeId, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<LifecycleOnboardingStatusResponse>($"/lifecycle/v1/employees/{employeeId}/onboarding/status", ct);
        return response is null ? null : ToChecklist(response);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateTaskAsync(Guid employeeId, UpdateOnboardingProgressRequest request, CancellationToken ct = default)
    {
        if (!request.IsCompleted)
        {
            return false;
        }

        var response = await httpClient.PutAsJsonAsync($"/lifecycle/v1/onboarding-items/{request.TaskId}/complete", new { Notes = (string?)null }, ct);
        return response.IsSuccessStatusCode;
    }

    private static OnboardingSummaryDto ToOnboardingSummary(LifecycleOnboardingStatusResponse response)
    {
        return new OnboardingSummaryDto
        {
            Id = response.Id,
            EmployeeId = response.EmployeeId,
            EmployeeName = string.Empty,
            Department = string.Empty,
            StartDate = response.StartDate,
            Progress = response.TotalItems <= 0 ? 0 : (int)Math.Round(response.CompletedItems * 100m / response.TotalItems),
            Buddy = string.Empty,
            Status = ReadJsonValue(response.Status)
        };
    }

    private static OnboardingChecklistDto ToChecklist(LifecycleOnboardingStatusResponse response)
    {
        return new OnboardingChecklistDto
        {
            Id = response.Id,
            EmployeeId = response.EmployeeId,
            Tasks = response.Items.Select(item => new OnboardingTaskDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description ?? string.Empty,
                IsCompleted = item.IsCompleted,
                AssignedTo = item.AssignedTo
            }).ToList()
        };
    }

    private static string ReadJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            _ => value.ToString()
        };
    }

    private sealed record LifecycleOnboardingStatusResponse(
        Guid Id,
        Guid EmployeeId,
        JsonElement Status,
        DateTime StartDate,
        int TotalItems,
        int CompletedItems,
        List<LifecycleOnboardingItemResponse> Items);

    private sealed record LifecycleOnboardingItemResponse(
        Guid Id,
        string Title,
        string? Description,
        Guid? AssignedTo,
        bool IsCompleted);
}
