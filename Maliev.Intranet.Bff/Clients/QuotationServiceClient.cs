using System.Text.Json;
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
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing quotation summaries.</returns>
    public async Task<PagedResponse<QuotationSummaryDto>?> GetQuotationsAsync(Guid? customerId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = $"/quotation/v1/quotations?page={page}&pageSize={pageSize}";
        if (customerId.HasValue) url += $"&customerId={customerId.Value}";
        return await httpClient.GetFromJsonAsync<PagedResponse<QuotationSummaryDto>>(url, ct);
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

    /// <summary>
    /// Creates a new quotation.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created quotation summary.</returns>
    public async Task<QuotationSummaryDto?> CreateQuotationAsync(CreateQuotationRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/quotation/v1/quotations", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<QuotationSummaryDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Updates an existing quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated quotation detail.</returns>
    public async Task<QuotationDetailDto?> UpdateQuotationAsync(Guid id, UpdateQuotationRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/quotation/v1/quotations/{id}", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<QuotationDetailDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Deletes a quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> DeleteQuotationAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/quotation/v1/quotations/{id}", ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Retrieves quotation metrics.
    /// </summary>
    public async Task<int> GetPendingQuotationCountAsync(CancellationToken ct = default)
    {
        var httpResponse = await httpClient.GetAsync("/quotation/v1/metrics/pending-count", ct);
        if (!httpResponse.IsSuccessStatusCode) return 0;
        var response = await httpResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return response.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }

    /// <summary>
    /// Updates the status of a quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/quotation/v1/quotations/{id}/status", new { status }, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Adds an internal note to a quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="request">The note request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    public async Task<HttpResponseMessage> AddNoteAsync(Guid id, AddQuotationNoteRequest request, CancellationToken ct = default)
    {
        return await httpClient.PostAsJsonAsync($"/quotation/v1/quotations/{id}/notes", request, ct);
    }

    /// <summary>
    /// Retrieves the count of sent quotations that have been awaiting customer response for more than <paramref name="minAgeDays"/> days.
    /// </summary>
    /// <param name="minAgeDays">Minimum age in days to consider a quotation aging.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The count of aging quotations.</returns>
    public async Task<int> GetAgingQuotationCountAsync(int minAgeDays = 7, CancellationToken ct = default)
    {
        var httpResponse = await httpClient.GetAsync($"/quotation/v1/metrics/aging-count?minAgeDays={minAgeDays}", ct);
        if (!httpResponse.IsSuccessStatusCode) return 0;
        var response = await httpResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
        return response.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }
}

