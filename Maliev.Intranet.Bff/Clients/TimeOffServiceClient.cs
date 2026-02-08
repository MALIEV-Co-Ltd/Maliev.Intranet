using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Employee microservice for leave/time-off data.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class TimeOffServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves leave balances for a specific employee.
    /// </summary>
    /// <param name="employeeId">The employee identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of leave balances.</returns>
    public async Task<List<LeaveBalanceDto>> GetLeaveBalancesAsync(Guid employeeId, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<MalievResponse<List<LeaveBalanceDto>>>($"/employee/v1/employees/{employeeId}/leave-balances", ct);
        return response?.Data ?? new();
    }

    /// <summary>
    /// Retrieves leave requests for a specific employee.
    /// </summary>
    /// <param name="employeeId">The employee identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of leave requests.</returns>
    public async Task<List<LeaveRequestSummaryDto>> GetLeaveRequestsAsync(Guid employeeId, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<MalievResponse<List<LeaveRequestSummaryDto>>>($"/employee/v1/employees/{employeeId}/leave-requests", ct);
        return response?.Data ?? new();
    }
}
