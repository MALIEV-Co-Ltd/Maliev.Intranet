using System.Net;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

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
    public async Task<PagedResponse<InvoiceSummaryDto>?> GetInvoicesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<InvoiceSummaryDto>>($"/invoice/v1/invoices?page={page}&pageSize={pageSize}", ct);
    }

    /// <summary>
    /// Retrieves detailed information for a single invoice by ID.
    /// </summary>
    public async Task<InvoiceDetailDto?> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<InvoiceDetailDto>($"/invoice/v1/invoices/{id}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a new invoice.
    /// </summary>
    public async Task<InvoiceSummaryDto?> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/invoice/v1/invoices", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<InvoiceSummaryDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Updates an existing invoice.
    /// </summary>
    public async Task<InvoiceDetailDto?> UpdateInvoiceAsync(Guid id, UpdateInvoiceRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/invoice/v1/invoices/{id}", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<InvoiceDetailDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Finalizes an invoice.
    /// </summary>
    public async Task<bool> FinalizeInvoiceAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/invoice/v1/invoices/{id}/finalize", null, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Cancels an invoice.
    /// </summary>
    public async Task<bool> CancelInvoiceAsync(Guid id, CancelInvoiceRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/invoice/v1/invoices/{id}/cancel", request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Splits an invoice into multiple child invoices.
    /// </summary>
    public async Task<List<InvoiceSummaryDto>?> SplitInvoiceAsync(Guid id, SplitInvoiceRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/invoice/v1/invoices/{id}/split", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InvoiceSummaryDto>>(cancellationToken: ct);
    }

    /// <summary>
    /// Creates a new billing note.
    /// </summary>
    public async Task<BillingNoteDto?> CreateBillingNoteAsync(CreateBillingNoteRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/invoice/v1/billing-notes", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BillingNoteDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Retrieves a paged list of billing notes, optionally filtered by invoice ID.
    /// </summary>
    public async Task<PagedResponse<BillingNoteDto>?> GetBillingNotesAsync(Guid? invoiceId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = $"/invoice/v1/billing-notes?page={page}&pageSize={pageSize}";
        if (invoiceId.HasValue)
            query += $"&invoiceId={invoiceId.Value}";
        return await httpClient.GetFromJsonAsync<PagedResponse<BillingNoteDto>>(query, ct);
    }

    /// <summary>
    /// Retrieves a billing note by ID.
    /// </summary>
    public async Task<BillingNoteDto?> GetBillingNoteByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<BillingNoteDto>($"/invoice/v1/billing-notes/{id}", ct);
    }

    /// <summary>
    /// Retrieves all active credit terms.
    /// </summary>
    public async Task<List<CreditTermDto>?> GetCreditTermsAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<CreditTermDto>>("/invoice/v1/credit-terms", ct);
    }
}
