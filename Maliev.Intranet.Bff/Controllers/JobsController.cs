using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for manufacturing job operations, proxying to the JobService.
/// </summary>
/// <param name="client">The typed JobService client.</param>
/// <param name="orderClient">The typed OrderService client, used to enrich job details with 6-sided previews.</param>
/// <param name="uploadClient">The typed UploadService client, used to resolve GCS storage paths to signed URLs.</param>
[RequirePermission(MalievPermissions.Job.Read, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class JobsController(JobServiceClient client, OrderServiceClient orderClient, UploadServiceClient uploadClient) : ControllerBase
{
    // ── Queue ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the full production queue view (all jobs grouped by status + stats).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The production queue DTO.</returns>
    [HttpGet("queue")]
    public async Task<ActionResult<ProductionQueueDto>> GetQueue(CancellationToken ct)
    {
        var result = await client.GetQueueAsync(ct);
        return result != null ? Ok(result) : Ok(new ProductionQueueDto());
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns aggregate job statistics (queued, in-progress, completed today, overdue, utilization).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stats DTO.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<JobStatsDto>> GetStats(CancellationToken ct)
    {
        var result = await client.GetStatsAsync(ct);
        return result != null ? Ok(result) : Ok(new JobStatsDto());
    }

    // ── Job list ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paged, filtered list of jobs.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="machineId">Optional machine ID filter.</param>
    /// <param name="processType">Optional process type filter.</param>
    /// <param name="priority">Optional priority filter.</param>
    /// <param name="page">Page number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of job summaries.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<JobSummaryDto>>> Get(
        [FromQuery] string? status = null,
        [FromQuery] Guid? machineId = null,
        [FromQuery] string? processType = null,
        [FromQuery] string? priority = null,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var result = await client.GetJobsAsync(status, machineId, processType, priority, page, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<JobSummaryDto>());
    }

    // ── Job detail ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns detailed information for a single job, including 6-sided part preview URLs resolved from OrderService.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The job detail DTO, or 404.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await client.GetJobByIdAsync(id, ct);
        if (result == null) return NotFound();

        // Enrich with 6-sided preview URLs from OrderService if the job has an associated order
        if (result.OrderId.HasValue)
        {
            var previews = await orderClient.GetPreviewImagesAsync(result.OrderId.Value, ct);
            foreach (var preview in previews)
            {
                var url = await uploadClient.GetDownloadUrlByPathAsync(preview.StoragePath, ct);
                if (string.IsNullOrEmpty(url)) continue;
                switch (preview.Side)
                {
                    case "Front":  result.PreviewFrontUrl  = url; break;
                    case "Back":   result.PreviewBackUrl   = url; break;
                    case "Left":   result.PreviewLeftUrl   = url; break;
                    case "Right":  result.PreviewRightUrl  = url; break;
                    case "Top":    result.PreviewTopUrl    = url; break;
                    case "Bottom": result.PreviewBottomUrl = url; break;
                }
            }
        }

        return Ok(result);
    }

    // ── QR code ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns QR code data for a job ticket.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The QR data DTO, or 404.</returns>
    [HttpGet("{id:guid}/qr")]
    public async Task<ActionResult<JobQrDto>> GetQr(Guid id, CancellationToken ct)
    {
        var result = await client.GetQrAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates a job's status (e.g. from Kanban drag-and-drop). Broadcasts the change via SignalR.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="request">The new status.</param>
    /// <param name="hub">The ProductionHub context for broadcasting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPatch("{id:guid}/status")]
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateJobStatusRequest request,
        [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Maliev.Intranet.Bff.Hubs.ProductionHub> hub,
        CancellationToken ct)
    {
        var response = await client.UpdateStatusAsync(id, request.Status, ct);
        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

        // Broadcast to all connected production queue clients
        await hub.Clients.All.SendAsync("JobStatusChanged", new { JobId = id, Status = request.Status });

        return NoContent();
    }

    /// <summary>
    /// Assigns a machine to a job. Broadcasts the assignment via SignalR.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="request">The machine assignment request.</param>
    /// <param name="hub">The ProductionHub context for broadcasting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("{id:guid}/assign-machine")]
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> AssignMachine(
        Guid id,
        [FromBody] AssignMachineRequest request,
        [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Maliev.Intranet.Bff.Hubs.ProductionHub> hub,
        CancellationToken ct)
    {
        var response = await client.AssignMachineAsync(id, request.MachineId, ct);
        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

        await hub.Clients.All.SendAsync("JobAssigned",
            new { JobId = id, MachineId = request.MachineId });

        return NoContent();
    }

    // ── Scheduling ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the scheduled jobs on a specific machine within a UTC date range.
    /// </summary>
    /// <param name="machineId">The machine identifier (asset code).</param>
    /// <param name="from">Range start (UTC). Defaults to today.</param>
    /// <param name="to">Range end (UTC). Defaults to 30 days from now.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of machine schedule items.</returns>
    [HttpGet("machine/{machineId}/schedule")]
    public async Task<ActionResult<List<MachineScheduleItemDto>>> GetMachineSchedule(
        string machineId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var rangeFrom = DateTime.SpecifyKind(from ?? DateTime.UtcNow.Date, DateTimeKind.Utc);
        var rangeTo = DateTime.SpecifyKind(to ?? DateTime.UtcNow.Date.AddDays(30), DateTimeKind.Utc);
        var result = await client.GetMachineScheduleAsync(machineId, rangeFrom, rangeTo, ct);
        return Ok(result);
    }

    /// <summary>
    /// Reorders a queued job to a new position in the machine queue. Broadcasts via SignalR.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="request">The reorder request (new position, 1-based).</param>
    /// <param name="hub">The ProductionHub context for broadcasting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPatch("{id:guid}/reorder")]
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> Reorder(
        Guid id,
        [FromBody] ReorderJobRequest request,
        [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Maliev.Intranet.Bff.Hubs.ProductionHub> hub,
        CancellationToken ct)
    {
        var response = await client.ReorderJobAsync(id, request.NewPosition, ct);
        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

        await hub.Clients.All.SendAsync("ScheduleChanged", new { JobId = id });

        return NoContent();
    }

    /// <summary>
    /// Returns schedules for all active machines within a UTC date range.
    /// Used by the Gantt planning view on the Production Queue page.
    /// </summary>
    /// <param name="facilityClient">FacilityService client (method-injected).</param>
    /// <param name="from">Range start (UTC). Defaults to today.</param>
    /// <param name="to">Range end (UTC). Defaults to 7 days from now.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>One summary per active machine, each with its list of scheduled jobs.</returns>
    [HttpGet("schedule")]
    public async Task<ActionResult<List<MachineScheduleSummaryDto>>> GetAllMachineSchedules(
        [FromServices] IFacilityServiceClient facilityClient,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var rangeFrom = DateTime.SpecifyKind(from ?? DateTime.UtcNow.Date, DateTimeKind.Utc);
        var rangeTo   = DateTime.SpecifyKind(to   ?? DateTime.UtcNow.Date.AddDays(7), DateTimeKind.Utc);

        var equipment = await facilityClient.GetEquipmentsAsync(status: "Active", pageSize: 200, ct: ct);
        var machines  = equipment?.Items ?? [];

        var scheduleTasks = machines.Select(async m =>
        {
            try
            {
                var slots = await client.GetMachineScheduleAsync(m.AssetCode, rangeFrom, rangeTo, ct);
                var items = slots.Select(s => new PlanningScheduleItemDto(
                    PlannedDate:      new DateTimeOffset(s.ScheduledStart, TimeSpan.Zero),
                    PlannedEndDate:   new DateTimeOffset(s.ScheduledEnd,   TimeSpan.Zero),
                    JobReference:     s.JobId.ToString("N")[..8].ToUpperInvariant(),
                    Status:           s.Status,
                    JobId:            s.JobId,
                    MachineName:      m.Name,
                    SetupTimeMinutes: s.SetupMinutes,
                    PrintTimeMinutes: s.PrintMinutes
                )).ToList();
                return new MachineScheduleSummaryDto(m.AssetCode, m.Name, m.Category, items);
            }
            catch
            {
                return new MachineScheduleSummaryDto(m.AssetCode, m.Name, m.Category, []);
            }
        });

        var results = await Task.WhenAll(scheduleTasks);
        return Ok(results.ToList());
    }
}
