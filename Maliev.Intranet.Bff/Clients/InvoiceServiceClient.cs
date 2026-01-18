using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Invoice microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class InvoiceServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of invoices.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing invoice summaries.</returns>
    public async Task<PagedResponse<InvoiceSummaryDto>?> GetInvoicesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<InvoiceSummaryDto>>($"/invoice/v1/invoices?page={page}&pageSize={pageSize}", ct);
    }

    /// <summary>
    /// Retrieves detailed information for a single invoice by ID.
    /// </summary>
    /// <param name="id">The invoice ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The invoice detail DTO.</returns>
    public async Task<InvoiceDetailDto?> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<InvoiceDetailDto>($"/invoice/v1/invoices/{id}", ct);
    }
}
