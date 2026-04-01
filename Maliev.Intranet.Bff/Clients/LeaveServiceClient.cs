using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Leave microservice.
/// </summary>
public interface ILeaveServiceClient
{
    /// <summary>
    /// Retrieves leave balances for an employee.
    /// </summary>
    Task<List<LeaveBalanceDto>> GetMyBalancesAsync(Guid employeeId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves leave requests for an employee.
    /// </summary>
    Task<List<LeaveRequestSummaryDto>> GetMyRequestsAsync(Guid employeeId, int? year, CancellationToken ct = default);

    /// <summary>
    /// Submits a new leave request.
    /// </summary>
    Task<LeaveRequestDetailDto?> SubmitRequestAsync(Guid employeeId, SubmitLeaveRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Gets pending approvals for a manager.
    /// </summary>
    Task<List<LeaveRequestDetailDto>> GetPendingApprovalsAsync(Guid managerId, CancellationToken ct = default);

    /// <summary>
    /// Approves or rejects a leave request.
    /// </summary>
    Task<bool> ProcessDecisionAsync(Guid requestId, Guid approverId, ApproveRejectLeaveRequest request, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the count of leave requests pending manager approval for a specific manager.
    /// </summary>
    Task<int> GetPendingApprovalCountAsync(Guid managerId, CancellationToken ct = default);
}

/// <summary>
/// Default implementation of the leave service client.
/// </summary>
public class LeaveServiceClient(HttpClient httpClient) : ILeaveServiceClient
{
    /// <inheritdoc />
    public async Task<List<LeaveBalanceDto>> GetMyBalancesAsync(Guid employeeId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/leave/v1/LeaveBalances/employee/{employeeId}", ct);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<LeaveBalanceDto>>(cancellationToken: ct) ?? [];
    }

    /// <inheritdoc />
    public async Task<List<LeaveRequestSummaryDto>> GetMyRequestsAsync(Guid employeeId, int? year, CancellationToken ct = default)
    {
        var url = $"/leave/v1/LeaveRequests/employee/{employeeId}";
        if (year.HasValue) url += $"?year={year.Value}";

        var response = await httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<LeaveRequestSummaryDto>>(cancellationToken: ct) ?? [];
    }

    /// <inheritdoc />
    public async Task<LeaveRequestDetailDto?> SubmitRequestAsync(Guid employeeId, SubmitLeaveRequestDto request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/leave/v1/LeaveRequests/{employeeId}", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<LeaveRequestDetailDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<List<LeaveRequestDetailDto>> GetPendingApprovalsAsync(Guid managerId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<LeaveRequestDetailDto>>($"/leave/v1/LeaveRequests/pending/{managerId}", ct) ?? [];
    }

    /// <inheritdoc />
    public async Task<bool> ProcessDecisionAsync(Guid requestId, Guid approverId, ApproveRejectLeaveRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/leave/v1/LeaveRequests/{requestId}/decision?approverId={approverId}", request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<int> GetPendingApprovalCountAsync(Guid managerId, CancellationToken ct = default)
    {
        var httpResponse = await httpClient.GetAsync($"/leave/v1/LeaveRequests/pending-count?managerId={managerId}", ct);
        if (!httpResponse.IsSuccessStatusCode) return 0;
        var response = await httpResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
        return response.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }
}
