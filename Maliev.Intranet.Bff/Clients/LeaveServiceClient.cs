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
}

/// <summary>
/// Default implementation of the leave service client.
/// </summary>
public class LeaveServiceClient(HttpClient httpClient) : ILeaveServiceClient
{
    /// <inheritdoc />
    public async Task<List<LeaveBalanceDto>> GetMyBalancesAsync(Guid employeeId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<LeaveBalanceDto>>($"/leave/v1/LeaveBalances/{employeeId}", ct) ?? [];
    }

    /// <inheritdoc />
    public async Task<List<LeaveRequestSummaryDto>> GetMyRequestsAsync(Guid employeeId, int? year, CancellationToken ct = default)
    {
        var url = $"/leave/v1/LeaveRequests/employee/{employeeId}";
        if (year.HasValue) url += $"?year={year.Value}";

        return await httpClient.GetFromJsonAsync<List<LeaveRequestSummaryDto>>(url, ct) ?? [];
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
}
