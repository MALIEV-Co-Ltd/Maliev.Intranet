using System.Text.Json;
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
        var alerts = await httpClient.GetFromJsonAsync<List<ComplianceAlertResponse>>("/compliance/v1/compliance-alerts?isResolved=false", ct) ?? [];
        var items = alerts.Select(ToComplianceRecord).ToList();
        var pageItems = items.Skip(Math.Max(page - 1, 0) * pageSize).Take(pageSize).ToList();

        return new PagedResponse<ComplianceRecordDto>
        {
            Data = pageItems,
            Meta = new PaginationMeta
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = items.Count,
                TotalItems = items.Count,
                TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(items.Count / (double)pageSize)
            }
        };
    }

    /// <inheritdoc />
    public async Task<ComplianceStatsDto?> GetComplianceStatsAsync(CancellationToken ct = default)
    {
        var report = await httpClient.GetFromJsonAsync<ComplianceReportResponse>("/compliance/v1/compliance-reports/compliance", ct);
        return report is null
            ? null
            : new ComplianceStatsDto
            {
                Expiring30Days = report.ExpiringSoon,
                Expiring60Days = report.ExpiringSoon + report.Expired,
                TotalActive = report.RequiresAuthorization
            };
    }

    /// <inheritdoc />
    public async Task<ComplianceRecordDto?> GetComplianceRecordByIdAsync(Guid id, CancellationToken ct = default)
    {
        var alerts = await httpClient.GetFromJsonAsync<List<ComplianceAlertResponse>>("/compliance/v1/compliance-alerts", ct) ?? [];
        return alerts.Where(alert => alert.Id == id).Select(ToComplianceRecord).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<ComplianceRecordDto?> CreateComplianceRecordAsync(CreateComplianceRecordRequest request, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteComplianceRecordAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/compliance/v1/compliance-alerts/{id}/resolve", new
        {
            ResolvedBy = Guid.Empty,
            ResolutionNotes = "Resolved from the intranet compliance records view."
        }, ct);

        return response.IsSuccessStatusCode;
    }

    private static ComplianceRecordDto ToComplianceRecord(ComplianceAlertResponse alert)
    {
        return new ComplianceRecordDto
        {
            Id = alert.Id,
            EmployeeId = alert.EmployeeId,
            Type = ReadJsonValue(alert.AlertType),
            Date = alert.CreatedDate,
            ExpiryDate = null,
            Status = alert.IsResolved ? "Resolved" : ReadJsonValue(alert.Severity)
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

    private sealed record ComplianceAlertResponse(
        Guid Id,
        Guid EmployeeId,
        JsonElement AlertType,
        JsonElement Severity,
        bool IsResolved,
        DateTime CreatedDate);

    private sealed record ComplianceReportResponse(
        int RequiresAuthorization,
        int ExpiringSoon,
        int Expired);
}
