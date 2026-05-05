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
        var response = await httpClient.GetFromJsonAsync<RecruitmentMetricsResponse>("/career/v1/reports/recruitment-metrics", ct);
        return response?.ToRecruitmentStats();
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
        var nameParts = request.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var downstreamRequest = new
        {
            request.JobPostingId,
            ApplicantFirstName = nameParts.Length > 0 ? nameParts[0] : request.FullName,
            ApplicantLastName = nameParts.Length > 1 ? nameParts[1] : ".",
            ApplicantEmail = request.Email,
            ApplicantPhone = request.Phone,
            ApplicantCountryCode = (string?)null,
            ResumeFileId = Guid.Empty,
            CoverLetter = (string?)null,
            AdditionalFileIds = Array.Empty<Guid>()
        };

        return await httpClient.PostAsJsonAsync("/career/v1/job-applications", downstreamRequest, ct);
    }

    /// <summary>
    /// Updates the status of a candidate application.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateCandidateStatusAsync(Guid id, UpdateCandidateStatusRequest request, CancellationToken ct = default)
    {
        var downstreamRequest = new
        {
            NewStatus = request.Status,
            Reason = request.Notes,
            IsReversal = false,
            request.RowVersion
        };

        return await httpClient.PatchAsJsonAsync($"/career/v1/job-applications/{id}/status", downstreamRequest, ct);
    }

    private sealed record RecruitmentMetricsResponse(
        int TotalApplications,
        Dictionary<string, decimal>? ConversionRates,
        int PositionsFilled,
        int PositionsOpen)
    {
        public RecruitmentStatsDto ToRecruitmentStats()
        {
            var rates = ConversionRates ?? [];
            return new RecruitmentStatsDto
            {
                Applied = TotalApplications,
                Screening = ReadRateCount(rates, "screen"),
                Interview = ReadRateCount(rates, "interview"),
                Offer = Math.Max(PositionsFilled, ReadRateCount(rates, "offer"))
            };
        }

        private static int ReadRateCount(Dictionary<string, decimal> rates, string keyFragment)
        {
            var match = rates.FirstOrDefault(pair => pair.Key.Contains(keyFragment, StringComparison.OrdinalIgnoreCase));
            return match.Equals(default(KeyValuePair<string, decimal>)) ? 0 : (int)Math.Round(match.Value);
        }
    }
}
