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
}
