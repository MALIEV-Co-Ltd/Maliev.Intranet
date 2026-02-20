using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client interface for interacting with the Performance microservice.
/// </summary>
public interface IPerformanceServiceClient
{
    /// <summary>
    /// Retrieves a list of performance reviews.
    /// </summary>
    Task<List<PerformanceReviewDto>?> GetReviewsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a list of goals.
    /// </summary>
    Task<List<GoalDto>?> GetGoalsAsync(CancellationToken ct = default);
}

/// <summary>
/// Client implementation for interacting with the Performance microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class PerformanceServiceClient(HttpClient httpClient) : IPerformanceServiceClient
{
    /// <inheritdoc />
    public async Task<List<PerformanceReviewDto>?> GetReviewsAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<PerformanceReviewDto>>("/performance/v1/reviews", ct);
    }

    /// <inheritdoc />
    public async Task<List<GoalDto>?> GetGoalsAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<GoalDto>>("/performance/v1/goals", ct);
    }
}
