using Asp.Versioning;
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
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RecruitmentController(CareerServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves active job postings.
    /// </summary>
    /// <returns>A list of job postings.</returns>
    [RequirePermission(MalievPermissions.Career.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("jobs")]
    public async Task<ActionResult<List<JobPostingSummaryDto>>> GetJobs()
    {
        var result = await client.GetJobPostingsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Retrieves recruitment pipeline statistics.
    /// </summary>
    /// <returns>Recruitment statistics.</returns>
    [RequirePermission(MalievPermissions.Career.Stats, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("stats")]
    public async Task<ActionResult<RecruitmentStatsDto>> GetStats()
    {
        var result = await client.GetRecruitmentStatsAsync();
        return result != null ? Ok(result) : Ok(new RecruitmentStatsDto());
    }

    /// <summary>
    /// Creates a new job posting.
    /// </summary>
    [RequirePermission(MalievPermissions.Career.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("jobs")]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobPostingRequest request, CancellationToken ct)
    {
        var response = await client.CreateJobPostingAsync(request, ct);
        return response.IsSuccessStatusCode ? StatusCode(201) : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Updates an existing job posting.
    /// </summary>
    [RequirePermission(MalievPermissions.Career.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("jobs/{id:guid}")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobPostingRequest request, CancellationToken ct)
    {
        var response = await client.UpdateJobPostingAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Creates a new candidate application.
    /// </summary>
    [RequirePermission(MalievPermissions.Career.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("candidates")]
    public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateRequest request, CancellationToken ct)
    {
        var response = await client.CreateCandidateAsync(request, ct);
        return response.IsSuccessStatusCode ? StatusCode(201) : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Updates the status of a candidate application.
    /// </summary>
    [RequirePermission(MalievPermissions.Career.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("candidates/{id:guid}/status")]
    public async Task<IActionResult> UpdateCandidateStatus(Guid id, [FromBody] UpdateCandidateStatusRequest request, CancellationToken ct)
    {
        var response = await client.UpdateCandidateStatusAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }
}
