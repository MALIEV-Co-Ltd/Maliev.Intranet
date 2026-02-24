using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Shared.Enums;
using Maliev.Aspire.ServiceDefaults.Authorization;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// BFF Controller for manufacturing jobs.
/// </summary>
[ApiController]
[Route("api/jobs")]
[RequirePermission(MalievPermissions.Job.Read)]
public class JobsController(IJobServiceClient jobClient) : ControllerBase
{
    /// <summary>Retrieves a specific job by ID.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<JobDto>> GetById(Guid id, CancellationToken ct)
    {
        var job = await jobClient.GetJobByIdAsync(id, ct);
        return job == null ? NotFound() : Ok(job);
    }

    /// <summary>Retrieves a paged list of jobs.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<JobDto>>> GetJobs(
        [FromQuery] JobStatus? status,
        [FromQuery] string? technology,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var response = await jobClient.GetJobsAsync(status, technology, page, pageSize, ct);
        return response == null ? StatusCode(502) : Ok(response);
    }

    /// <summary>Retrieves the Kanban board view of jobs.</summary>
    [HttpGet("kanban")]
    public async Task<ActionResult<KanbanResponse>> GetKanban(CancellationToken ct)
    {
        var response = await jobClient.GetKanbanAsync(ct);
        return response == null ? StatusCode(502) : Ok(response);
    }

    /// <summary>Queues a job on a specific machine.</summary>
    [HttpPost("{id}/queue")]
    [RequirePermission(MalievPermissions.Job.Write)]
    public async Task<ActionResult<JobDto>> Queue(Guid id, [FromBody] QueueJobRequest request, CancellationToken ct)
    {
        var job = await jobClient.QueueJobAsync(id, request, ct);
        return job == null ? StatusCode(502, "Job service unavailable") : Ok(job);
    }

    /// <summary>Starts a job.</summary>
    [HttpPost("{id}/start")]
    [RequirePermission(MalievPermissions.Job.Write)]
    public async Task<ActionResult<JobDto>> Start(Guid id, CancellationToken ct)
    {
        var job = await jobClient.StartJobAsync(id, ct);
        return job == null ? StatusCode(502, "Job service unavailable") : Ok(job);
    }

    /// <summary>Moves a job to finishing stage.</summary>
    [HttpPost("{id}/finish")]
    [RequirePermission(MalievPermissions.Job.Write)]
    public async Task<ActionResult<JobDto>> Finish(Guid id, CancellationToken ct)
    {
        var job = await jobClient.FinishJobAsync(id, ct);
        return job == null ? StatusCode(502, "Job service unavailable") : Ok(job);
    }

    /// <summary>Completes a job.</summary>
    [HttpPost("{id}/complete")]
    [RequirePermission(MalievPermissions.Job.Write)]
    public async Task<ActionResult<JobDto>> Complete(Guid id, CancellationToken ct)
    {
        var job = await jobClient.CompleteJobAsync(id, ct);
        return job == null ? StatusCode(502, "Job service unavailable") : Ok(job);
    }

    /// <summary>Cancels a job.</summary>
    [HttpPost("{id}/cancel")]
    [RequirePermission(MalievPermissions.Job.Write)]
    public async Task<ActionResult<JobDto>> Cancel(Guid id, [FromBody] CancelJobRequest request, CancellationToken ct)
    {
        var job = await jobClient.CancelJobAsync(id, request, ct);
        return job == null ? StatusCode(502, "Job service unavailable") : Ok(job);
    }
}
