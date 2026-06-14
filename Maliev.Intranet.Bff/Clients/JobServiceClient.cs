using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using QRCoder;

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

    /// <summary>
    /// Retrieves the number of production jobs whose estimated completion has passed.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of overdue production jobs.</returns>
    public async Task<int> GetOverdueJobCountAsync(CancellationToken ct = default)
    {
        var queue = await GetQueueAsync(ct);
        return queue?.Stats.OverdueCount ?? 0;
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
        var job = await response.Content.ReadFromJsonAsync<JobServiceJobResponse>(cancellationToken: ct);
        return job?.ToJobDetail();
    }

    // ── Status & assignment ───────────────────────────────────────────────────

    /// <summary>
    /// Updates editable details for a manufacturing job.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="request">The editable detail changes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response from JobService.</returns>
    public async Task<HttpResponseMessage> UpdateDetailsAsync(Guid id, UpdateJobDetailsRequest request, CancellationToken ct = default) =>
        await httpClient.PatchAsJsonAsync($"/job/v1/jobs/{id}/details", request, ct);

    /// <summary>
    /// Updates the status of a job (e.g. from Kanban drag-and-drop).
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="newStatus">The new status string.</param>
    /// <param name="machineId">The machine identifier required when queueing a pending job.</param>
    /// <param name="cancellationReason">The operator-provided cancellation reason when cancelling a job.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response.</returns>
    public async Task<HttpResponseMessage> UpdateStatusAsync(
        Guid id,
        string newStatus,
        string? machineId = null,
        string? cancellationReason = null,
        CancellationToken ct = default)
    {
        return newStatus.Trim().ToLowerInvariant() switch
        {
            "queued" or "queue" when !string.IsNullOrWhiteSpace(machineId) => await httpClient.PostAsJsonAsync($"/job/v1/jobs/{id}/queue", new { MachineId = machineId }, ct),
            "queued" or "queue" => new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "MachineId is required to queue a job."
            },
            "inprogress" or "in progress" or "started" or "start" => await httpClient.PostAsync($"/job/v1/jobs/{id}/start", null, ct),
            "finishing" or "qualitycheck" or "quality check" or "packaging" => await httpClient.PostAsync($"/job/v1/jobs/{id}/finish", null, ct),
            "completed" or "complete" => await httpClient.PostAsync($"/job/v1/jobs/{id}/complete", null, ct),
            "cancelled" or "canceled" or "cancel" => await httpClient.PostAsJsonAsync(
                $"/job/v1/jobs/{id}/cancel",
                new { Reason = NormalizeCancellationReason(cancellationReason) },
                ct),
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

    /// <summary>
    /// Retrieves tentative production planning holds from JobService.
    /// </summary>
    /// <param name="projectId">Optional project filter.</param>
    /// <param name="technology">Optional manufacturing technology filter.</param>
    /// <param name="activeOnly">True to return only active non-expired holds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching planning holds.</returns>
    public async Task<List<ProductionPlanningHoldDto>> GetPlanningHoldsAsync(
        Guid? projectId = null,
        string? technology = null,
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        var query = $"/job/v1/jobs/planning-holds?activeOnly={activeOnly.ToString().ToLowerInvariant()}";
        if (projectId.HasValue) query += $"&projectId={projectId.Value}";
        if (!string.IsNullOrWhiteSpace(technology)) query += $"&technology={Uri.EscapeDataString(technology)}";

        var response = await httpClient.GetAsync(query, ct);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<ProductionPlanningHoldDto>>(cancellationToken: ct) ?? [];
    }

    /// <summary>
    /// Creates a tentative production planning hold.
    /// </summary>
    /// <param name="request">The hold request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response from JobService.</returns>
    public async Task<HttpResponseMessage> CreatePlanningHoldAsync(CreateProductionPlanningHoldRequest request, CancellationToken ct = default) =>
        await httpClient.PostAsJsonAsync("/job/v1/jobs/planning-holds", request, ct);

    /// <summary>
    /// Updates a tentative production planning hold.
    /// </summary>
    /// <param name="holdId">The hold identifier.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response from JobService.</returns>
    public async Task<HttpResponseMessage> UpdatePlanningHoldAsync(Guid holdId, UpdateProductionPlanningHoldRequest request, CancellationToken ct = default) =>
        await httpClient.PatchAsJsonAsync($"/job/v1/jobs/planning-holds/{holdId}", request, ct);

    /// <summary>
    /// Cancels a tentative production planning hold.
    /// </summary>
    /// <param name="holdId">The hold identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response from JobService.</returns>
    public async Task<HttpResponseMessage> CancelPlanningHoldAsync(Guid holdId, CancellationToken ct = default) =>
        await httpClient.DeleteAsync($"/job/v1/jobs/planning-holds/{holdId}", ct);

    private static string NormalizeCancellationReason(string? reason)
    {
        reason = reason?.Trim();
        return string.IsNullOrWhiteSpace(reason)
            ? "Cancelled from Intranet status update."
            : reason;
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
        ct.ThrowIfCancellationRequested();

        string url = $"/mfg/production-schedule?jobId={id:D}";
        using var qrGenerator = new QRCodeGenerator();
        using QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrBytes = qrCode.GetGraphic(8);

        return new JobQrDto
        {
            JobId = id,
            Url = url,
            QrPngBase64 = Convert.ToBase64String(qrBytes)
        };
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
    /// Gets scheduled jobs and active planning holds grouped by machine.
    /// </summary>
    /// <param name="from">Range start in UTC.</param>
    /// <param name="to">Range end in UTC.</param>
    /// <param name="machineIds">Optional machine identifiers to include.</param>
    /// <param name="technologies">Optional technologies to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Machine schedule groups from JobService.</returns>
    public async Task<List<(string MachineId, IReadOnlyList<MachineScheduleItemDto> Schedule)>> GetScheduleAsync(
        DateTime from,
        DateTime to,
        IEnumerable<string>? machineIds = null,
        IEnumerable<string>? technologies = null,
        CancellationToken ct = default)
    {
        var query = new List<string>
        {
            $"from={Uri.EscapeDataString(from.ToString("O"))}",
            $"to={Uri.EscapeDataString(to.ToString("O"))}"
        };

        if (machineIds is not null)
        {
            query.AddRange(machineIds
                .Where(machineId => !string.IsNullOrWhiteSpace(machineId))
                .Select(machineId => $"machineIds={Uri.EscapeDataString(machineId)}"));
        }

        if (technologies is not null)
        {
            query.AddRange(technologies
                .Where(technology => !string.IsNullOrWhiteSpace(technology))
                .Select(technology => $"technologies={Uri.EscapeDataString(technology)}"));
        }

        var response = await httpClient.GetAsync($"/job/v1/jobs/schedule?{string.Join("&", query)}", ct);
        if (!response.IsSuccessStatusCode) return [];

        var result = await response.Content.ReadFromJsonAsync<List<JobServiceMachineScheduleResponse>>(cancellationToken: ct) ?? [];
        return result
            .Select(machine => (machine.MachineId, (IReadOnlyList<MachineScheduleItemDto>)machine.Schedule))
            .ToList();
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

    /// <summary>
    /// Moves a queued job to a specific schedule slot.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <param name="request">The schedule move request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response.</returns>
    public async Task<HttpResponseMessage> RescheduleJobAsync(Guid id, RescheduleJobRequest request, CancellationToken ct = default) =>
        await httpClient.PatchAsJsonAsync($"/job/v1/jobs/{id}/schedule", request, ct);

    private sealed record JobServiceMachineScheduleResponse(
        string MachineId,
        List<MachineScheduleItemDto> Schedule);

    private sealed record JobServiceJobResponse
    {
        public Guid JobId { get; init; }

        public Guid OrderId { get; init; }

        public Guid OrderItemId { get; init; }

        public Guid? SourceProjectId { get; init; }

        public Guid? SourceProjectPartId { get; init; }

        public Guid MaterialId { get; init; }

        public string? CustomerId { get; init; }

        public string? CustomerName { get; init; }

        public string? Technology { get; init; }

        public int EstimatedPrintTimeMinutes { get; init; }

        public string? AssignedMachineId { get; init; }

        public string? AssignedOperator { get; init; }

        public int Priority { get; init; }

        public string? Status { get; init; }

        public string? Notes { get; init; }

        public DateTime? StartedAt { get; init; }

        public DateTime? CompletedAt { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime UpdatedAt { get; init; }

        public DateTime? ScheduledStartTime { get; init; }

        public DateTime? ScheduledEndTime { get; init; }

        public int QueuePosition { get; init; }

        public JobDetailDto ToJobDetail()
        {
            var processType = Technology ?? string.Empty;
            return new JobDetailDto
            {
                Id = JobId,
                JobNumber = FormatShortId(JobId),
                CustomerId = string.IsNullOrWhiteSpace(CustomerId) ? null : CustomerId,
                CustomerName = CustomerName ?? string.Empty,
                OrderId = OrderId == Guid.Empty ? null : OrderId,
                OrderNumber = OrderId == Guid.Empty ? null : FormatShortId(OrderId),
                PartDescription = FormatPartDescription(SourceProjectPartId, OrderItemId),
                ProcessType = processType,
                Material = MaterialId == Guid.Empty ? null : MaterialId.ToString(),
                MaterialId = MaterialId == Guid.Empty ? null : MaterialId,
                Priority = FormatPriority(Priority),
                Status = Status ?? string.Empty,
                MachineName = AssignedMachineId,
                EstimatedCompletionAt = ScheduledEndTime ?? StartedAt?.AddMinutes(EstimatedPrintTimeMinutes),
                ProgressPercent = FormatProgressPercent(Status),
                Quantity = 1,
                CreatedAt = CreatedAt,
                ScheduledStartTime = ScheduledStartTime,
                ScheduledEndTime = ScheduledEndTime,
                QueuePosition = QueuePosition,
                AssignedTo = AssignedOperator,
                Notes = Notes,
                UpdatedAt = UpdatedAt
            };
        }
    }

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

    private static string FormatShortId(Guid id) =>
        id.ToString("N")[..8].ToUpperInvariant();

    private static string FormatPartDescription(Guid? sourceProjectPartId, Guid orderItemId)
    {
        if (sourceProjectPartId is { } projectPartId && projectPartId != Guid.Empty)
        {
            return $"Project part {FormatShortId(projectPartId)}";
        }

        return orderItemId == Guid.Empty
            ? string.Empty
            : $"Order item {FormatShortId(orderItemId)}";
    }

    private static string FormatPriority(int priority) => priority switch
    {
        <= 1 => "Urgent",
        2 => "High",
        3 => "Normal",
        _ => "Low"
    };

    private static int FormatProgressPercent(string? status) => status?.Trim() switch
    {
        "Completed" or "Complete" => 100,
        "Finishing" or "QualityCheck" or "Packaging" => 80,
        "InProgress" or "InProduction" => 50,
        "Queued" => 10,
        _ => 0
    };
}
