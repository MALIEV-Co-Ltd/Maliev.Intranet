using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Supplier microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class SupplierServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of suppliers.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing supplier summaries.</returns>
    public async Task<PagedResponse<SupplierSummaryDto>?> GetSuppliersAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<SupplierSummaryDto>>($"/supplier/v1/suppliers?page={page}&pageSize={pageSize}", ct);
    }

    /// <summary>
    /// Retrieves detailed information for a single supplier by ID.
    /// </summary>
    /// <param name="id">The supplier ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The supplier detail DTO.</returns>
    public async Task<SupplierDetailDto?> GetSupplierByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<SupplierDetailDto>($"/supplier/v1/suppliers/{id}", ct);
    }
}
