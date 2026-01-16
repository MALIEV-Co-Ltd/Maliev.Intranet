using System.Net.Http.Json;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Payment microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class PaymentServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of payments.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing payment summaries.</returns>
    public async Task<PagedResponse<PaymentSummaryDto>?> GetPaymentsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<PaymentSummaryDto>>($"/payment/v1/payments?page={page}&pageSize={pageSize}", ct);
    }

    /// <summary>
    /// Retrieves detailed information for a single payment by ID.
    /// </summary>
    /// <param name="id">The payment ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The payment detail DTO.</returns>
    public async Task<PaymentDetailDto?> GetPaymentByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PaymentDetailDto>($"/payment/v1/payments/{id}", ct);
    }
}
