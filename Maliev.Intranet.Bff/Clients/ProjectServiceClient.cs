using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Typed HTTP client for interacting with the ProjectService downstream API.
/// </summary>
/// <param name="httpClient">The HTTP client configured by <c>AddBffServiceClient</c>.</param>
public class ProjectServiceClient(HttpClient httpClient)
{
    // ── Query endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves a paged list of projects with optional filtering.
    /// </summary>
    /// <param name="status">Optional status filter (e.g. "Configuring").</param>
    /// <param name="search">Optional text search (project number, title, or customer name).</param>
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paged response of project summaries.</returns>
    public async Task<PagedResponse<ProjectSummaryDto>> GetProjectsAsync(
        string? status = null, string? search = null, Guid? customerId = null,
        int page = 1, int pageSize = 20,
        CancellationToken ct = default)
    {
        var url = $"/project/v1/projects?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (customerId.HasValue) url += $"&customerId={customerId.Value}";

        var response = await httpClient.GetFromJsonAsync<PagedResponse<ProjectSummaryDto>>(url, ct);
        return response ?? new PagedResponse<ProjectSummaryDto>();
    }

    /// <summary>
    /// Retrieves full detail for a single project by ID.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The project detail DTO, or <c>null</c> if not found.</returns>
    public async Task<ProjectDetailDto?> GetProjectByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/project/v1/projects/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProjectDetailDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Returns project statistics for the dashboard action items panel.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated project counts by status.</returns>
    public async Task<ProjectStatsDto?> GetProjectStatsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/project/v1/projects/stats", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProjectStatsDto>(cancellationToken: ct);
    }

    // ── Mutation endpoints ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a new project.
    /// </summary>
    public async Task<(ProjectDetailDto? Result, string? ErrorContent, int StatusCode)> CreateProjectAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/project/v1/projects", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                return (null, errorContent, (int)response.StatusCode);
            }
            var result = await response.Content.ReadFromJsonAsync<ProjectDetailDto>(cancellationToken: ct);
            return (result, null, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return (null, $"Exception calling ProjectService: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Updates an existing project's metadata (title, description, notes, validity).
    /// </summary>
    public async Task<HttpResponseMessage> UpdateProjectAsync(Guid id, object request, CancellationToken ct = default)
        => await httpClient.PutAsJsonAsync($"/project/v1/projects/{id}", request, ct);

    /// <summary>
    /// Deletes a project (only permitted when in Draft status).
    /// </summary>
    public async Task<HttpResponseMessage> DeleteProjectAsync(Guid id, CancellationToken ct = default)
        => await httpClient.DeleteAsync($"/project/v1/projects/{id}", ct);

    // ── Part endpoints ───────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new part to an existing project.
    /// Maps the Intranet DTO to the ProjectService DTO shape (field name differences).
    /// </summary>
    public async Task<(ProjectPartDto? Result, string? ErrorContent, int StatusCode)> AddPartAsync(Guid projectId, AddProjectPartRequest request, CancellationToken ct = default)
    {
        var payload = new
        {
            request.FileId,
            request.FileReference,
            request.FileName,
            ProcessType = MapManufacturingProcess(request.ProcessType),
            request.MaterialId,
            request.Quantity,
            FinishType = request.Finish,
            request.Color,
            request.Tolerance,
        };

        var response = await httpClient.PostAsJsonAsync($"/project/v1/projects/{projectId}/parts", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            return (null, errorContent, (int)response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<ProjectPartDto>(cancellationToken: ct);
        return (result, null, (int)response.StatusCode);
    }

    /// <summary>
    /// Updates the configuration of an existing part.
    /// Maps the Intranet DTO to the ProjectService DTO shape (field name differences).
    /// </summary>
    public async Task<HttpResponseMessage> UpdatePartAsync(Guid projectId, Guid partId, UpdateProjectPartRequest request, CancellationToken ct = default)
    {
        var payload = new
        {
            ProcessType = MapManufacturingProcess(request.ProcessType),
            request.MaterialId,
            request.Quantity,
            FinishType = request.Finish,
            request.Color,
            request.Tolerance,
        };

        return await httpClient.PutAsJsonAsync($"/project/v1/projects/{projectId}/parts/{partId}", payload, ct);
    }

    private static int? MapManufacturingProcess(string? processType)
    {
        if (string.IsNullOrWhiteSpace(processType))
            return null;

        var normalized = processType.Trim()
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .ToUpperInvariant();

        return normalized switch
        {
            "FDM" => 1,
            "SLA" or "DLP" or "SLA_DLP" => 2,
            "SLS" => 3,
            "CNC" or "CNC_MILL" or "CNC_MILLING" or "MILLING" => 10,
            "CNC_TURN" or "CNC_TURNING" or "TURNING" or "LATHE" => 11,
            "CNC_5AXIS" or "CNC_5_AXIS" or "5_AXIS" => 12,
            "SHEET_METAL" or "SHEETMETAL" or "SHEET_METAL_CUTTING" => 20,
            "SHEET_METAL_BENDING" => 21,
            "SHEET_METAL_WELDING" => 22,
            "INJECTION" or "INJECTION_MOLDING" or "INJECTIONMOULDING" => 30,
            "3D_SCANNING" or "THREEDSCANNING" => 40,
            "DESIGN" => 50,
            "ASSEMBLY" or "FINISHING" => 60,
            _ => null,
        };
    }

    /// <summary>
    /// Removes a part from a project.
    /// </summary>
    public async Task<HttpResponseMessage> DeletePartAsync(Guid projectId, Guid partId, CancellationToken ct = default)
        => await httpClient.DeleteAsync($"/project/v1/projects/{projectId}/parts/{partId}", ct);

    // ── Pricing endpoints ────────────────────────────────────────────────────

    /// <summary>
    /// Requests AI price estimation for a specific part. Returns the detailed price breakdown.
    /// </summary>
    public async Task<ProjectPriceBreakdownDto?> GetPartPriceAsync(Guid projectId, Guid partId, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/project/v1/projects/{projectId}/parts/{partId}/price", null, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProjectPriceBreakdownDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Confirms or overrides the price for a specific part.
    /// </summary>
    public async Task<HttpResponseMessage> ConfirmPartPriceAsync(Guid projectId, Guid partId, ConfirmPartPriceRequest request, CancellationToken ct = default)
        => await httpClient.PostAsJsonAsync($"/project/v1/projects/{projectId}/parts/{partId}/confirm-price", request, ct);

    // ── Quotation lifecycle endpoints ────────────────────────────────────────

    /// <summary>
    /// Generates a quotation PDF for the project. Only allowed when all parts have confirmed prices.
    /// </summary>
    public async Task<HttpResponseMessage> GenerateQuotationAsync(Guid projectId, CancellationToken ct = default)
        => await httpClient.PostAsync($"/project/v1/projects/{projectId}/generate-quotation", null, ct);

    /// <summary>
    /// Marks a project's quotation as accepted by the customer.
    /// </summary>
    public async Task<HttpResponseMessage> AcceptQuotationAsync(Guid projectId, CancellationToken ct = default)
        => await httpClient.PostAsync($"/project/v1/projects/{projectId}/accept-quotation", null, ct);

    // ── Dashboard helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the count of projects currently in Configuring status (waiting for pricing).
    /// Used by the dashboard action items panel.
    /// </summary>
    public async Task<int> GetConfiguringCountAsync(CancellationToken ct = default)
    {
        var stats = await GetProjectStatsAsync(ct);
        return stats?.ConfiguringCount ?? 0;
    }
}
