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
    private static readonly JsonSerializerOptions PreferenceJsonOptions = new(JsonSerializerDefaults.Web);

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
        var downstreamRequest = new
        {
            EmployeeNumber = $"EMP-{DateTime.UtcNow:yyyyMMddHHmmss}",
            request.FirstName,
            request.LastName,
            WorkEmail = request.Email,
            DateOfBirth = DateTime.UtcNow.Date.AddYears(-18),
            StartDate = request.StartDate == default ? DateTime.UtcNow.Date : request.StartDate,
            EmploymentType = "FullTime",
            JobTitle = request.Title,
            MobilePhone = (string?)null
        };

        var response = await httpClient.PostAsJsonAsync("/employee/v1/hr/employees", downstreamRequest, ct);
        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var employeeId = created.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var id)
                ? id
                : Guid.Empty;

            return new EmployeeDetailDto
            {
                Id = employeeId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Department = request.Department,
                Title = request.Title,
                Status = "Active",
                HireDate = request.StartDate == default ? DateTime.UtcNow.Date : request.StartDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        return null;
    }

    /// <summary>
    /// Updates an existing employee.
    /// </summary>
    public virtual async Task<EmployeeDetailDto?> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/employee/v1/profile/{id}/profile", new
        {
            PersonalEmail = (string?)null,
            MobilePhone = request.Phone,
            PreferredName = (string?)null
        }, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<EmployeeDetailDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Retrieves the EmployeeService self-service profile for an employee.
    /// </summary>
    public virtual async Task<EmployeeSelfProfileDto?> GetSelfServiceProfileAsync(Guid employeeId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/employee/v1/profile/{employeeId}/profile", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmployeeSelfProfileDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Updates EmployeeService self-service profile fields for an employee.
    /// </summary>
    public virtual async Task<bool> UpdateSelfServiceProfileAsync(Guid employeeId, UpdateEmployeeSelfProfileRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/employee/v1/profile/{employeeId}/profile", request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        response.EnsureSuccessStatusCode();
        return true;
    }

    /// <summary>
    /// Terminates an employee.
    /// </summary>
    public virtual async Task<bool> TerminateEmployeeAsync(Guid id, TerminateEmployeeRequest request, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return false;
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
        var downstreamPreference = await response.Content.ReadFromJsonAsync<EmployeePreferenceResponse>(cancellationToken: ct);
        return MapPreference(downstreamPreference);
    }

    /// <summary>
    /// Updates a user preference.
    /// </summary>
    public virtual async Task<UserPreferenceDto?> UpsertPreferenceAsync(string scope, UpsertPreferenceRequest request, CancellationToken ct = default)
    {
        var downstreamRequest = new EmployeeUpsertPreferenceRequest
        {
            PreferenceData = JsonSerializer.Serialize(request.PreferenceData, PreferenceJsonOptions)
        };

        var response = await httpClient.PutAsJsonAsync($"/employee/v1/preferences/{scope}", downstreamRequest, PreferenceJsonOptions, ct);
        response.EnsureSuccessStatusCode();
        var downstreamPreference = await response.Content.ReadFromJsonAsync<EmployeePreferenceResponse>(cancellationToken: ct);
        return MapPreference(downstreamPreference);
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
        var response = await httpClient.GetAsync("/employee/v1/reports/org-chart", ct);
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
        await Task.CompletedTask;
        return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Employee notes are not exposed by EmployeeService."
        };
    }

    /// <summary>
    /// Retrieves an employee profile by IAM Principal ID.
    /// </summary>
    public virtual async Task<EmployeeDetailDto?> GetByPrincipalIdAsync(Guid principalId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/employee/v1/employees/by-principal/{principalId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<EmployeeSelfProfileDto>(cancellationToken: ct);
        return profile is null ? null : MapSelfProfile(profile);
    }

    private static EmployeeDetailDto MapSelfProfile(EmployeeSelfProfileDto profile)
    {
        return new EmployeeDetailDto
        {
            Id = profile.Id,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Email = profile.WorkEmail,
            Phone = profile.MobilePhone,
            Department = profile.DepartmentName ?? string.Empty,
            Title = profile.JobTitle ?? string.Empty,
            Status = profile.EmploymentStatus,
            ManagerName = profile.ManagerName,
            HireDate = profile.StartDate ?? profile.CreatedAt,
            WorkLocation = profile.WorkLocation,
            EmployeeType = profile.EmploymentType,
            EmergencyContacts = profile.EmergencyContacts,
            CreatedAt = profile.CreatedAt ?? DateTime.MinValue,
            UpdatedAt = profile.CreatedAt ?? DateTime.MinValue
        };
    }

    private static UserPreferenceDto? MapPreference(EmployeePreferenceResponse? preference)
    {
        if (preference is null)
        {
            return null;
        }

        return new UserPreferenceDto
        {
            PrincipalId = preference.PrincipalId,
            Scope = preference.Scope,
            PreferenceData = DeserializePreferenceData(preference.PreferenceData),
            UpdatedAt = preference.UpdatedAt
        };
    }

    private static Dictionary<string, object> DeserializePreferenceData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, PreferenceJsonOptions);
            return data?.ToDictionary(pair => pair.Key, pair => (object)pair.Value.Clone()) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record EmployeeUpsertPreferenceRequest
    {
        public string PreferenceData { get; init; } = "{}";
    }

    private sealed record EmployeePreferenceResponse
    {
        public Guid PrincipalId { get; init; }

        public string Scope { get; init; } = string.Empty;

        public string PreferenceData { get; init; } = "{}";

        public DateTime UpdatedAt { get; init; }
    }
}
