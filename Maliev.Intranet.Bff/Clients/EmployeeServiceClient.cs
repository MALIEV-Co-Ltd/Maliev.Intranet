using System.Text.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Employee microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class EmployeeServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of employees.
    /// </summary>
    public virtual async Task<PagedResponse<EmployeeSummaryDto>?> GetEmployeesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/employee/v1/employees?page={page}&pageSize={pageSize}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return new PagedResponse<EmployeeSummaryDto>();
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResponse<EmployeeSummaryDto>>(cancellationToken: ct);
    }

    /// <summary>
    /// Retrieves detailed information for a single employee by ID.
    /// </summary>
    public virtual async Task<EmployeeDetailDto?> GetEmployeeByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/employee/v1/employees/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmployeeDetailDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Creates a new employee.
    /// </summary>
    public virtual async Task<EmployeeDetailDto?> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/employee/v1/employees", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<EmployeeDetailDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Updates an existing employee.
    /// </summary>
    public virtual async Task<EmployeeDetailDto?> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/employee/v1/employees/{id}", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<EmployeeDetailDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Terminates an employee.
    /// </summary>
    public virtual async Task<bool> TerminateEmployeeAsync(Guid id, TerminateEmployeeRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/employee/v1/employees/{id}/terminate", request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Retrieves employee metrics.
    /// </summary>
    public virtual async Task<int> GetTotalHeadcountAsync(CancellationToken ct = default)
    {
        var httpResponse = await httpClient.GetAsync("/employee/v1/metrics/headcount", ct);
        if (!httpResponse.IsSuccessStatusCode) return 0;
        var response = await httpResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return response.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }

    /// <summary>
    /// Gets a user preference by scope.
    /// </summary>
    public virtual async Task<UserPreferenceDto?> GetPreferenceAsync(string scope, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/employee/v1/preferences/{scope}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserPreferenceDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Updates a user preference.
    /// </summary>
    public virtual async Task<UserPreferenceDto?> UpsertPreferenceAsync(string scope, UpsertPreferenceRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/employee/v1/preferences/{scope}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserPreferenceDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Retrieves HR analytics data.
    /// </summary>
    public virtual async Task<HrAnalyticsDto?> GetHrAnalyticsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/employee/v1/analytics/summary", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return new HrAnalyticsDto();
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HrAnalyticsDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Retrieves the organizational chart data.
    /// </summary>
    public virtual async Task<List<OrgNodeDto>?> GetOrgChartAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/employee/v1/employees/org-chart", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return [];
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<OrgNodeDto>>(cancellationToken: ct);
    }

    /// <summary>
    /// Deletes a user preference.
    /// </summary>
    public virtual async Task DeletePreferenceAsync(string scope, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/employee/v1/preferences/{scope}", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Adds an internal HR note to an employee.
    /// </summary>
    public virtual async Task<HttpResponseMessage> AddNoteAsync(Guid id, AddEmployeeNoteRequest request, CancellationToken ct = default)
    {
        return await httpClient.PostAsJsonAsync($"/employee/v1/employees/{id}/notes", request, ct);
    }

    /// <summary>
    /// Retrieves an employee profile by IAM Principal ID.
    /// </summary>
    public virtual async Task<EmployeeDetailDto?> GetByPrincipalIdAsync(Guid principalId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/employee/v1/employees/by-principal/{principalId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmployeeDetailDto>(cancellationToken: ct);
    }
}
