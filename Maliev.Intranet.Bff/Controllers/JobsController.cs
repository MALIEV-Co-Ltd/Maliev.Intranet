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
[RequirePermission(MalievPermissions.Job.Read, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class JobsController(JobServiceClient client) : ControllerBase
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
    /// Returns detailed information for a single job.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The job detail DTO, or 404.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await client.GetJobByIdAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
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
}
