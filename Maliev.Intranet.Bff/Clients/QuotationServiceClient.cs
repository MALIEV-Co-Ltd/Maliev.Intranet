using System.Net.Http.Json;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Quotation microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class QuotationServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of quotations.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing quotation summaries.</returns>
    public async Task<PagedResponse<QuotationSummaryDto>?> GetQuotationsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<QuotationSummaryDto>>($"/quotation/v1/quotations?page={page}&pageSize={pageSize}", ct);
    }

    /// <summary>
    /// Retrieves detailed information for a single quotation by ID.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The quotation detail DTO.</returns>
    public async Task<QuotationDetailDto?> GetQuotationByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<QuotationDetailDto>($"/quotation/v1/quotations/{id}", ct);
    }
}
