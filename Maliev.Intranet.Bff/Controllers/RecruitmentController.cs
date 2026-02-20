using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for recruitment-related operations, proxying to the Career Service.
/// </summary>
/// <param name="client">The career service client.</param>
[ApiController]
[Route("api/[controller]")]
public class RecruitmentController(CareerServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves active job postings.
    /// </summary>
    /// <returns>A list of job postings.</returns>
    [HttpGet("jobs")]
    [RequirePermission(MalievPermissions.Career.Read, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<List<JobPostingSummaryDto>>> GetJobs()
    {
        var result = await client.GetJobPostingsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Retrieves recruitment pipeline statistics.
    /// </summary>
    /// <returns>Recruitment statistics.</returns>
    [HttpGet("stats")]
    [RequirePermission(MalievPermissions.Career.Stats, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<RecruitmentStatsDto>> GetStats()
    {
        var result = await client.GetRecruitmentStatsAsync();
        return result != null ? Ok(result) : Ok(new RecruitmentStatsDto());
    }

    /// <summary>
    /// Creates a new job posting.
    /// </summary>
    [HttpPost("jobs")]
    [RequirePermission(MalievPermissions.Career.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobPostingRequest request, CancellationToken ct)
    {
        var response = await client.CreateJobPostingAsync(request, ct);
        return response.IsSuccessStatusCode ? StatusCode(201) : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Updates an existing job posting.
    /// </summary>
    [HttpPut("jobs/{id:guid}")]
    [RequirePermission(MalievPermissions.Career.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobPostingRequest request, CancellationToken ct)
    {
        var response = await client.UpdateJobPostingAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Creates a new candidate application.
    /// </summary>
    [HttpPost("candidates")]
    [RequirePermission(MalievPermissions.Career.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateRequest request, CancellationToken ct)
    {
        var response = await client.CreateCandidateAsync(request, ct);
        return response.IsSuccessStatusCode ? StatusCode(201) : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Updates the status of a candidate application.
    /// </summary>
    [HttpPatch("candidates/{id:guid}/status")]
    [RequirePermission(MalievPermissions.Career.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> UpdateCandidateStatus(Guid id, [FromBody] UpdateCandidateStatusRequest request, CancellationToken ct)
    {
        var response = await client.UpdateCandidateStatusAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }
}
