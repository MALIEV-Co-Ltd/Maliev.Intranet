using System.Security.Cryptography;
using System.Text;

using Asp.Versioning;
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
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
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
                    case "Front": result.PreviewFrontUrl = url; break;
                    case "Back": result.PreviewBackUrl = url; break;
                    case "Left": result.PreviewLeftUrl = url; break;
                    case "Right": result.PreviewRightUrl = url; break;
                    case "Top": result.PreviewTopUrl = url; break;
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
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}/status")]
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
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/assign-machine")]
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
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}/reorder")]
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
    public async Task<ActionResult<ProductionScheduleBoardDto>> GetAllMachineSchedules(
        [FromServices] IFacilityServiceClient facilityClient,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var rangeFrom = DateTime.SpecifyKind(from ?? DateTime.UtcNow.Date, DateTimeKind.Utc);
        var rangeTo = DateTime.SpecifyKind(to ?? DateTime.UtcNow.Date.AddDays(7), DateTimeKind.Utc);

        var equipment = await facilityClient.GetEquipmentsAsync(pageSize: 200, ct: ct);
        var machines = (equipment?.Items ?? [])
            .Where(IsProductionScheduleMachine)
            .ToList();
        var machineIds = machines.Select(machine => machine.AssetCode).Where(code => !string.IsNullOrWhiteSpace(code)).ToList();
        var technologies = machines.Select(machine => MapEquipmentCategoryToTechnology(machine.Category)).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var scheduleGroups = await client.GetScheduleAsync(rangeFrom, rangeTo, machineIds, technologies, ct);

        return Ok(BuildScheduleBoard(rangeFrom, rangeTo, machines, scheduleGroups));
    }

    /// <summary>
    /// Moves a queued job to a specific schedule slot and broadcasts the schedule change.
    /// </summary>
    /// <param name="id">The job GUID.</param>
    /// <param name="request">The schedule move request.</param>
    /// <param name="hub">The ProductionHub context for broadcasting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}/schedule")]
    public async Task<IActionResult> Reschedule(
        Guid id,
        [FromBody] RescheduleJobRequest request,
        [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Maliev.Intranet.Bff.Hubs.ProductionHub> hub,
        CancellationToken ct)
    {
        var response = await client.RescheduleJobAsync(id, request, ct);
        if (!response.IsSuccessStatusCode) return await ForwardDownstreamFailureAsync(response, ct);

        await hub.Clients.All.SendAsync("ScheduleChanged", new { MachineId = request.MachineId });
        return NoContent();
    }

    /// <summary>
    /// Updates a tentative planning hold from the production scheduler.
    /// </summary>
    /// <param name="holdId">The planning hold identifier.</param>
    /// <param name="request">The updated hold schedule.</param>
    /// <param name="hub">The ProductionHub context for broadcasting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("planning-holds/{holdId:guid}")]
    public async Task<IActionResult> UpdatePlanningHold(
        Guid holdId,
        [FromBody] UpdateProductionPlanningHoldRequest request,
        [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Maliev.Intranet.Bff.Hubs.ProductionHub> hub,
        CancellationToken ct)
    {
        var response = await client.UpdatePlanningHoldAsync(holdId, request, ct);
        if (!response.IsSuccessStatusCode) return await ForwardDownstreamFailureAsync(response, ct);

        await hub.Clients.All.SendAsync("ScheduleChanged", new { MachineId = request.MachineId });
        return NoContent();
    }

    private static async Task<IActionResult> ForwardDownstreamFailureAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var statusCode = (int)response.StatusCode;
        var content = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(content))
            return new StatusCodeResult(statusCode);

        return new ContentResult
        {
            StatusCode = statusCode,
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }

    private static ProductionScheduleBoardDto BuildScheduleBoard(
        DateTime rangeFrom,
        DateTime rangeTo,
        IReadOnlyList<EquipmentSummaryDto> machines,
        IReadOnlyList<(string MachineId, IReadOnlyList<MachineScheduleItemDto> Schedule)> scheduleGroups)
    {
        var scheduleByMachine = scheduleGroups
            .GroupBy(group => group.MachineId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(item => item.Schedule).ToList(),
                StringComparer.OrdinalIgnoreCase);

        return new ProductionScheduleBoardDto
        {
            RangeStart = rangeFrom,
            RangeEnd = rangeTo,
            Machines = machines
                .OrderBy(machine => machine.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(machine => machine.Name, StringComparer.OrdinalIgnoreCase)
                .Select(machine =>
                {
                    scheduleByMachine.TryGetValue(machine.AssetCode, out var slots);
                    var mappedSlots = (slots ?? [])
                        .Select(slot => MapScheduleSlot(machine, slot, currentProjectId: null, partFileNames: null))
                        .ToList();
                    mappedSlots.AddRange(BuildMaintenanceSlots(machine, rangeFrom, rangeTo));

                    return new ProductionScheduleMachineDto
                    {
                        MachineId = machine.AssetCode,
                        MachineName = machine.Name,
                        Category = machine.Category,
                        Technology = MapEquipmentCategoryToTechnology(machine.Category),
                        Slots = mappedSlots
                            .OrderBy(slot => slot.ScheduledStart)
                            .ThenBy(slot => slot.IsMaintenance ? 0 : 1)
                            .ToList()
                    };
                })
                .ToList()
        };
    }

    private static IEnumerable<ProductionScheduleSlotDto> BuildMaintenanceSlots(
        EquipmentSummaryDto machine,
        DateTime rangeFrom,
        DateTime rangeTo)
    {
        if (machine.NextServiceDueDate is not { } dueDate)
            yield break;

        var scheduledStart = dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var scheduledEnd = scheduledStart.AddDays(1);
        if (scheduledEnd <= rangeFrom || scheduledStart >= rangeTo)
            yield break;

        yield return new ProductionScheduleSlotDto
        {
            SlotId = CreateMaintenanceSlotId(machine.Id, dueDate),
            MachineId = machine.AssetCode,
            MachineName = machine.Name,
            Technology = MapEquipmentCategoryToTechnology(machine.Category),
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledEnd,
            Status = "Maintenance",
            Label = "Maintenance due",
            FileName = FirstNonEmpty(machine.ModelName, machine.Brand, machine.Name),
            IsMaintenance = true,
            CanMove = false
        };
    }

    private static ProductionScheduleSlotDto MapScheduleSlot(
        EquipmentSummaryDto machine,
        MachineScheduleItemDto slot,
        Guid? currentProjectId,
        IReadOnlyDictionary<Guid, string>? partFileNames)
    {
        var isCurrentProject = currentProjectId.HasValue && slot.ProjectId == currentProjectId.Value;
        var slotId = slot.HoldId ?? slot.JobId;
        var label = slot.IsHold
            ? $"Hold #{slot.QueuePosition}"
            : slot.JobId.ToString("N")[..8].ToUpperInvariant();

        return new ProductionScheduleSlotDto
        {
            SlotId = slotId,
            JobId = slot.IsHold ? null : slot.JobId,
            HoldId = slot.HoldId,
            ProjectId = slot.ProjectId,
            ProjectPartId = slot.ProjectPartId,
            FileName = slot.ProjectPartId.HasValue && partFileNames?.TryGetValue(slot.ProjectPartId.Value, out var fileName) == true ? fileName : null,
            MachineId = machine.AssetCode,
            MachineName = machine.Name,
            Technology = slot.Technology,
            ScheduledStart = DateTime.SpecifyKind(slot.ScheduledStart, DateTimeKind.Utc),
            ScheduledEnd = DateTime.SpecifyKind(slot.ScheduledEnd, DateTimeKind.Utc),
            SetupMinutes = slot.SetupMinutes,
            ProductionMinutes = slot.PrintMinutes,
            QueuePosition = slot.QueuePosition,
            Status = slot.Status,
            Label = label,
            ExpiresAt = slot.ExpiresAt,
            IsHold = slot.IsHold,
            IsCurrentProject = isCurrentProject,
            CanMove = slot.IsHold || string.Equals(slot.Status, "Queued", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool IsProductionScheduleMachine(EquipmentSummaryDto machine) =>
        !string.IsNullOrWhiteSpace(machine.AssetCode)
        && !string.IsNullOrWhiteSpace(MapEquipmentCategoryToTechnology(machine.Category))
        && !IsTerminalEquipmentStatus(machine.Status);

    private static bool IsTerminalEquipmentStatus(string? status) =>
        string.Equals(status, "Lost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Decommissioned", StringComparison.OrdinalIgnoreCase);

    private static Guid CreateMaintenanceSlotId(Guid equipmentId, DateOnly dueDate)
    {
        var source = $"{equipmentId:N}:{dueDate:yyyyMMdd}:maintenance";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string MapEquipmentCategoryToTechnology(string? category) => category switch
    {
        "FdmPrinter" => "FDM",
        "SlaPrinter" => "SLA",
        "CncMachine" => "CNC_MILL",
        "InjectionMolding" => "INJECTION_MOLDING",
        _ => string.Empty
    };
}
