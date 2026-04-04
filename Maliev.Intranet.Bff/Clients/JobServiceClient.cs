using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the JobService microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance (pre-configured with base address and auth).</param>
public class JobServiceClient(HttpClient httpClient)
{
    // ── Queue view ────────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves the full production queue view — jobs grouped and ordered for the Kanban board.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The production queue DTO, or <c>null</c> on failure.</returns>
    public async Task<ProductionQueueDto?> GetQueueAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/job/v1/jobs/queue", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProductionQueueDto>(cancellationToken: ct);
    }

    // ── Job list & detail ─────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves a paged list of jobs, optionally filtered by status, machine or process type.
    /// </summary>
    /// <param name="status">Optional status filter (e.g. "Queued", "InProgress").</param>
    /// <param name="machineId">Optional machine ID filter.</param>
    /// <param name="processType">Optional process type filter (e.g. "FDM", "CNC").</param>
    /// <param name="priority">Optional priority filter (e.g. "Urgent", "High").</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of job summaries.</returns>
    public async Task<PagedResponse<JobSummaryDto>?> GetJobsAsync(
        string? status = null,
        Guid? machineId = null,
        string? processType = null,
        string? priority = null,
        int page = 1,
        CancellationToken ct = default)
    {
        var qs = $"/job/v1/jobs?page={page}";
        if (!string.IsNullOrEmpty(status))      qs += $"&status={Uri.EscapeDataString(status)}";
        if (machineId.HasValue)                 qs += $"&machineId={machineId.Value}";
        if (!string.IsNullOrEmpty(processType)) qs += $"&processType={Uri.EscapeDataString(processType)}";
        if (!string.IsNullOrEmpty(priority))    qs += $"&priority={Uri.EscapeDataString(priority)}";

        return await httpClient.GetFromJsonAsync<PagedResponse<JobSummaryDto>>(qs, ct);
    }

    /// <summary>
    /// Retrieves detailed information for a single job.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The job detail DTO, or <c>null</c> if not found.</returns>
    public async Task<JobDetailDto?> GetJobByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/job/v1/jobs/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JobDetailDto>(cancellationToken: ct);
    }

    // ── Status & assignment ───────────────────────────────────────────────────

    /// <summary>
    /// Updates the status of a job (e.g. from Kanban drag-and-drop).
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="newStatus">The new status string.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response.</returns>
    public async Task<HttpResponseMessage> UpdateStatusAsync(Guid id, string newStatus, CancellationToken ct = default) =>
        await httpClient.PatchAsJsonAsync($"/job/v1/jobs/{id}/status", new { Status = newStatus }, ct);

    /// <summary>
    /// Assigns a machine to a job.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="machineId">The machine GUID to assign.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response.</returns>
    public async Task<HttpResponseMessage> AssignMachineAsync(Guid id, Guid machineId, CancellationToken ct = default) =>
        await httpClient.PostAsJsonAsync($"/job/v1/jobs/{id}/assign", new { MachineId = machineId }, ct);

    // ── Queue depth ───────────────────────────────────────────────────────────

    /// <summary>
    /// Gets queue depth (active job count) by technology.
    /// </summary>
    /// <param name="technology">Optional technology filter (e.g., "FDM", "CNC_MILL").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary of technology to active job count, or null on failure.</returns>
    public async Task<Dictionary<string, int>?> GetQueueDepthAsync(
        string? technology = null, CancellationToken ct = default)
    {
        var url = "/job/v1/jobs/queue-depth";
        if (!string.IsNullOrEmpty(technology))
            url += $"?technology={Uri.EscapeDataString(technology)}";

        var response = await httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<Dictionary<string, int>>(cancellationToken: ct);
    }

    // ── Stats & QR ────────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves aggregate production statistics (queued count, in-progress, completed today, overdue).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stats DTO, or <c>null</c> on failure.</returns>
    public async Task<JobStatsDto?> GetStatsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/job/v1/jobs/stats", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<JobStatsDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Retrieves QR code data for a job ticket (job ID encoded as a URL-safe string).
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The QR data DTO, or <c>null</c> on failure.</returns>
    public async Task<JobQrDto?> GetQrAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/job/v1/jobs/{id}/qr", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<JobQrDto>(cancellationToken: ct);
    }
}
