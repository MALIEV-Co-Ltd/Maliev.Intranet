using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Career microservice for recruitment data.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class CareerServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves active job postings from the Career service.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of job posting summaries.</returns>
    public async Task<List<JobPostingSummaryDto>> GetJobPostingsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<MalievResponse<List<JobPostingSummaryDto>>>("/career/v1/job-postings", ct);
        return response?.Data ?? new();
    }

    /// <summary>
    /// Retrieves recruitment statistics from the Career service.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Recruitment statistics.</returns>
    public async Task<RecruitmentStatsDto?> GetRecruitmentStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/career/v1/reports/recruitment-metrics", ct);
            if (!response.IsSuccessStatusCode) return null;

            var element = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            return new RecruitmentStatsDto
            {
                Applied = element.TryGetProperty("totalApplications", out var ta) ? ta.GetInt32() : 0,
                Screening = element.TryGetProperty("positionsOpen", out var po) ? po.GetInt32() : 0,
                Interview = 0,
                Offer = element.TryGetProperty("positionsFilled", out var pf) ? pf.GetInt32() : 0
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a new job posting.
    /// </summary>
    public async Task<HttpResponseMessage> CreateJobPostingAsync(CreateJobPostingRequest request, CancellationToken ct = default)
    {
        return await httpClient.PostAsJsonAsync("/career/v1/job-postings", request, ct);
    }

    /// <summary>
    /// Updates an existing job posting.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateJobPostingAsync(Guid id, UpdateJobPostingRequest request, CancellationToken ct = default)
    {
        return await httpClient.PutAsJsonAsync($"/career/v1/job-postings/{id}", request, ct);
    }

    /// <summary>
    /// Creates a new candidate application for a job posting.
    /// </summary>
    public async Task<HttpResponseMessage> CreateCandidateAsync(CreateCandidateRequest request, CancellationToken ct = default)
    {
        return await httpClient.PostAsJsonAsync("/career/v1/candidates", request, ct);
    }

    /// <summary>
    /// Updates the status of a candidate application.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateCandidateStatusAsync(Guid id, UpdateCandidateStatusRequest request, CancellationToken ct = default)
    {
        return await httpClient.PatchAsJsonAsync($"/career/v1/candidates/{id}/status", request, ct);
    }
}
