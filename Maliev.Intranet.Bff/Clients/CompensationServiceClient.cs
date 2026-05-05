using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client interface for interacting with the Compensation microservice.
/// </summary>
public interface ICompensationServiceClient
{
    /// <summary>
    /// Retrieves compensation summary.
    /// </summary>
    Task<CompensationSummaryDto?> GetSummaryAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a list of benefits.
    /// </summary>
    Task<List<BenefitDto>?> GetBenefitsAsync(CancellationToken ct = default);
}

/// <summary>
/// Client implementation for interacting with the Compensation microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class CompensationServiceClient(HttpClient httpClient) : ICompensationServiceClient
{
    /// <inheritdoc />
    public async Task<CompensationSummaryDto?> GetSummaryAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<CompensationAnalysisResponse>("/compensation/v1/reports/compensation-analysis", ct);
        return response is null
            ? null
            : new CompensationSummaryDto
            {
                BaseSalary = response.AverageSalary,
                AnnualBonus = 0m,
                TotalCompensation = response.TotalAnnualBudget,
                PercentageChange = 0m
            };
    }

    /// <inheritdoc />
    public async Task<List<BenefitDto>?> GetBenefitsAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<BenefitDto>>("/compensation/v1/benefits", ct);
    }

    private sealed record CompensationAnalysisResponse(decimal TotalAnnualBudget, decimal AverageSalary);
}
