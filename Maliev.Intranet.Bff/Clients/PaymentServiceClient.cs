using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Payment microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class PaymentServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves payment metrics.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Payment statistics DTO.</returns>
    public async Task<PaymentStatsDto?> GetPaymentStatsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/payment/v1/metrics/stats", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PaymentStatsDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Retrieves a paged list of payments.
    /// </summary>

    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing payment summaries.</returns>
    public async Task<PagedResponse<PaymentSummaryDto>?> GetPaymentsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/payment/v1/payments?page={page}&pageSize={pageSize}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PagedResponse<PaymentSummaryDto>>(cancellationToken: ct);
    }

    /// <summary>
    /// Retrieves detailed information for a single payment by ID.
    /// </summary>
    /// <param name="id">The payment ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The payment detail DTO.</returns>
    public async Task<PaymentDetailDto?> GetPaymentByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/payment/v1/payments/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentDetailDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Records a new payment.
    /// </summary>
    public async Task<HttpResponseMessage> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct = default)
    {
        return await httpClient.PostAsJsonAsync("/payment/v1/payments", request, ct);
    }

    /// <summary>
    /// Allocates a payment to invoices.
    /// </summary>
    public async Task<HttpResponseMessage> AllocatePaymentAsync(Guid id, AllocatePaymentRequest request, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Payment allocation is not exposed by PaymentService."
        };
    }

    /// <summary>
    /// Voids a payment.
    /// </summary>
    public async Task<HttpResponseMessage> VoidPaymentAsync(Guid id, VoidPaymentRequest request, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Payment voiding is not exposed by PaymentService."
        };
    }
}
