using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client interface for interacting with the Compliance microservice.
/// </summary>
public interface IComplianceServiceClient
{
    /// <summary>
    /// Retrieves a paged list of compliance records.
    /// </summary>
    Task<PagedResponse<ComplianceRecordDto>?> GetComplianceRecordsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>
    /// Retrieves compliance statistics.
    /// </summary>
    Task<ComplianceStatsDto?> GetComplianceStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a compliance record by ID.
    /// </summary>
    Task<ComplianceRecordDto?> GetComplianceRecordByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new compliance record.
    /// </summary>
    Task<ComplianceRecordDto?> CreateComplianceRecordAsync(CreateComplianceRecordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a compliance record.
    /// </summary>
    Task<bool> DeleteComplianceRecordAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Client implementation for interacting with the Compliance microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class ComplianceServiceClient(HttpClient httpClient) : IComplianceServiceClient
{
    /// <inheritdoc />
    public async Task<PagedResponse<ComplianceRecordDto>?> GetComplianceRecordsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<ComplianceRecordDto>>($"/compliance/v1/records?page={page}&pageSize={pageSize}", ct);
    }

    /// <inheritdoc />
    public async Task<ComplianceStatsDto?> GetComplianceStatsAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<ComplianceStatsDto>("/compliance/v1/records/stats", ct);
    }

    /// <inheritdoc />
    public async Task<ComplianceRecordDto?> GetComplianceRecordByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<ComplianceRecordDto>($"/compliance/v1/records/{id}", ct);
    }

    /// <inheritdoc />
    public async Task<ComplianceRecordDto?> CreateComplianceRecordAsync(CreateComplianceRecordRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/compliance/v1/records", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ComplianceRecordDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteComplianceRecordAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/compliance/v1/records/{id}", ct);
        return response.IsSuccessStatusCode;
    }
}
