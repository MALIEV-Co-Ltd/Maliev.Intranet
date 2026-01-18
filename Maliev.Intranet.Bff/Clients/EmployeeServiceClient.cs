using Maliev.Intranet.Shared;

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
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing employee summaries.</returns>
    public async Task<PagedResponse<EmployeeSummaryDto>?> GetEmployeesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<EmployeeSummaryDto>>($"/employee/v1/employees?page={page}&pageSize={pageSize}", ct);
    }

    /// <summary>
    /// Retrieves detailed information for a single employee by ID.
    /// </summary>
    /// <param name="id">The employee ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The employee detail DTO.</returns>
    public async Task<EmployeeDetailDto?> GetEmployeeByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<EmployeeDetailDto>($"/employee/v1/employees/{id}", ct);
    }
}
