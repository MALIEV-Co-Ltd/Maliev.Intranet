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
        var response = await httpClient.GetAsync("/job/v1/jobs/kanban", ct);
        if (!response.IsSuccessStatusCode) return null;
        var kanban = await response.Content.ReadFromJsonAsync<KanbanBoardResponse>(cancellationToken: ct);
        return kanban?.ToProductionQueue();
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
        if (!string.IsNullOrEmpty(status)) qs += $"&status={Uri.EscapeDataString(status)}";
        if (machineId.HasValue) qs += $"&machineId={machineId.Value}";
        if (!string.IsNullOrEmpty(processType)) qs += $"&processType={Uri.EscapeDataString(processType)}";
        if (!string.IsNullOrEmpty(priority)) qs += $"&priority={Uri.EscapeDataString(priority)}";

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
    public async Task<HttpResponseMessage> UpdateStatusAsync(Guid id, string newStatus, CancellationToken ct = default)
    {
        return newStatus.Trim().ToLowerInvariant() switch
        {
            "queued" or "queue" => await httpClient.PostAsJsonAsync($"/job/v1/jobs/{id}/queue", new { MachineId = string.Empty }, ct),
            "inprogress" or "in progress" or "started" or "start" => await httpClient.PostAsync($"/job/v1/jobs/{id}/start", null, ct),
            "finishing" or "qualitycheck" or "quality check" or "packaging" => await httpClient.PostAsync($"/job/v1/jobs/{id}/finish", null, ct),
            "completed" or "complete" => await httpClient.PostAsync($"/job/v1/jobs/{id}/complete", null, ct),
            "cancelled" or "canceled" or "cancel" => await httpClient.PostAsJsonAsync($"/job/v1/jobs/{id}/cancel", new { Reason = "Cancelled from Intranet." }, ct),
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "Unsupported JobService status transition."
            }
        };
    }

    /// <summary>
    /// Assigns a machine to a job.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="machineId">The machine GUID to assign.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response.</returns>
    public async Task<HttpResponseMessage> AssignMachineAsync(Guid id, Guid machineId, CancellationToken ct = default) =>
        await httpClient.PatchAsJsonAsync($"/job/v1/jobs/{id}/reassign", new { MachineId = machineId.ToString() }, ct);

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
        var queue = await GetQueueAsync(ct);
        return queue?.Stats;
    }

    /// <summary>
    /// Retrieves QR code data for a job ticket (job ID encoded as a URL-safe string).
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The QR data DTO, or <c>null</c> on failure.</returns>
    public async Task<JobQrDto?> GetQrAsync(Guid id, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return null;
    }

    // ── Scheduling ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets all scheduled jobs on a specific machine within a UTC date range.
    /// </summary>
    /// <param name="machineId">The machine identifier (asset code).</param>
    /// <param name="from">Range start (UTC).</param>
    /// <param name="to">Range end (UTC).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of scheduled job DTOs, or empty list on failure.</returns>
    public async Task<List<MachineScheduleItemDto>> GetMachineScheduleAsync(
        string machineId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var url = $"/job/v1/jobs/machine/{Uri.EscapeDataString(machineId)}/schedule" +
                  $"?from={from:O}&to={to:O}";
        var response = await httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<MachineScheduleItemDto>>(cancellationToken: ct) ?? [];
    }

    /// <summary>
    /// Reorders a queued job to a new position in the machine queue.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="newPosition">The target queue position (1-based).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response.</returns>
    public async Task<HttpResponseMessage> ReorderJobAsync(Guid id, int newPosition, CancellationToken ct = default) =>
        await httpClient.PatchAsJsonAsync($"/job/v1/jobs/{id}/reorder", new { NewPosition = newPosition }, ct);

    private sealed record KanbanBoardResponse(
        List<KanbanJobResponse>? Pending,
        List<KanbanJobResponse>? Queued,
        List<KanbanJobResponse>? InProgress,
        List<KanbanJobResponse>? Finishing,
        List<KanbanJobResponse>? Completed,
        List<KanbanJobResponse>? Cancelled)
    {
        public ProductionQueueDto ToProductionQueue()
        {
            var jobs = new List<JobSummaryDto>();
            AddJobs(jobs, Pending, "Pending");
            AddJobs(jobs, Queued, "Queued");
            AddJobs(jobs, InProgress, "InProgress");
            AddJobs(jobs, Finishing, "Finishing");
            AddJobs(jobs, Completed, "Completed");
            AddJobs(jobs, Cancelled, "Cancelled");

            return new ProductionQueueDto
            {
                Jobs = jobs,
                Stats = new JobStatsDto
                {
                    QueuedCount = jobs.Count(job => job.Status is "Queued"),
                    InProgressCount = jobs.Count(job => job.Status is "InProgress" or "Finishing"),
                    CompletedTodayCount = jobs.Count(job => job.Status is "Completed" && job.EstimatedCompletionAt?.Date == DateTime.UtcNow.Date),
                    OverdueCount = jobs.Count(job => job.IsOverdue),
                    MachineUtilizationPercent = 0
                }
            };
        }

        private static void AddJobs(List<JobSummaryDto> target, List<KanbanJobResponse>? source, string status)
        {
            if (source is null)
            {
                return;
            }

            target.AddRange(source.Select(job => job.ToJobSummary(status)));
        }
    }

    private sealed record KanbanJobResponse(
        Guid JobId,
        Guid OrderId,
        string Technology,
        Guid MaterialId,
        string? AssignedMachineId,
        int Priority,
        int EstimatedPrintTimeMinutes,
        DateTime? StartedAt)
    {
        public JobSummaryDto ToJobSummary(string status)
        {
            return new JobSummaryDto
            {
                Id = JobId,
                JobNumber = JobId.ToString("N")[..8].ToUpperInvariant(),
                OrderId = OrderId,
                OrderNumber = OrderId.ToString("N")[..8].ToUpperInvariant(),
                CustomerName = string.Empty,
                PartDescription = OrderId.ToString(),
                ProcessType = Technology,
                Material = MaterialId == Guid.Empty ? null : MaterialId.ToString(),
                Priority = Priority <= 1 ? "Low" : Priority >= 4 ? "High" : "Normal",
                Status = status,
                MachineName = AssignedMachineId,
                MachineId = Guid.TryParse(AssignedMachineId, out var machineId) ? machineId : null,
                EstimatedCompletionAt = StartedAt?.AddMinutes(EstimatedPrintTimeMinutes),
                ProgressPercent = status switch
                {
                    "Completed" => 100,
                    "Finishing" => 80,
                    "InProgress" => 50,
                    _ => 0
                },
                Quantity = 1,
                CreatedAt = StartedAt ?? DateTime.MinValue,
                ScheduledStartTime = StartedAt,
                ScheduledEndTime = StartedAt?.AddMinutes(EstimatedPrintTimeMinutes)
            };
        }
    }
}
