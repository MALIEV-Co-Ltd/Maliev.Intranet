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
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paged list of invoices.</returns>
    public async Task<PagedResponse<InvoiceSummaryDto>?> GetInvoicesAsync(Guid? customerId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = $"/invoice/v1/invoices?page={page}&pageSize={pageSize}";
        if (customerId.HasValue) url += $"&customerId={customerId.Value}";
        var response = await httpClient.GetFromJsonAsync<InvoiceServicePaginatedResponse<InvoiceServiceInvoiceResponse>>(url, ct);
        return response?.ToPagedResponse(invoice => invoice.ToSummary()) ?? new PagedResponse<InvoiceSummaryDto>();
    }

    /// <summary>
    /// Retrieves detailed information for a single invoice by ID.
    /// </summary>
    public async Task<InvoiceDetailDto?> GetInvoiceByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var invoice = await httpClient.GetFromJsonAsync<InvoiceServiceInvoiceResponse>($"/invoice/v1/invoices/{id}", ct);
            return invoice?.ToDetail();
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
        var response = await httpClient.PostAsJsonAsync("/invoice/v1/invoices", new
        {
            customerId = request.CustomerId,
            billingIdentityType = request.BillingIdentityType,
            customerName = request.CustomerName,
            customerTaxId = request.CustomerTaxId,
            billingAddress = request.BillingAddress,
            shippingAddress = request.ShippingAddress,
            poNumber = request.PoNumber,
            currency = request.Currency,
            issueDate = request.IssueDate,
            dueDate = request.DueDate,
            paymentTermsDays = request.PaymentTermsDays,
            lines = request.Items.Select((item, index) => new
            {
                lineNumber = index + 1,
                description = item.Description,
                quantity = item.Quantity,
                unitPrice = item.UnitPrice,
                taxCategory = item.TaxRate > 0 ? "VAT" : "Exempt",
                taxRate = item.TaxRate
            }).ToList()
        }, ct);
        if (response.IsSuccessStatusCode)
        {
            var invoice = await response.Content.ReadFromJsonAsync<InvoiceServiceInvoiceResponse>(cancellationToken: ct);
            return invoice?.ToSummary();
        }
        return null;
    }

    /// <summary>
    /// Registers an invoice file reference after upload.
    /// </summary>
    public async Task<InvoiceFileReferenceDto?> RegisterFileAsync(Guid invoiceId, RegisterInvoiceFileRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/invoice/v1/invoices/{invoiceId}/files", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<InvoiceFileReferenceDto>(cancellationToken: ct);
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

    /// <summary>
    /// Retrieves the count of overdue invoices.
    /// </summary>
    public async Task<int> GetOverdueInvoiceCountAsync(CancellationToken ct = default)
    {
        var httpResponse = await httpClient.GetAsync("/invoice/v1/metrics/overdue-count", ct);
        if (!httpResponse.IsSuccessStatusCode) return 0;
        var response = await httpResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
        return response.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }

    /// <summary>
    /// Returns the first invoice matching the given customer PO number, or <c>null</c> if none found.
    /// Used by the order lifecycle aggregator to link an order to its invoice without a direct foreign key.
    /// </summary>
    /// <param name="poNumber">The customer PO number stored on the order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The first matching invoice summary, or <c>null</c>.</returns>
    public async Task<InvoiceSummaryDto?> GetFirstInvoiceByPoNumberAsync(string poNumber, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"/invoice/v1/invoices?poNumber={Uri.EscapeDataString(poNumber)}&pageSize=1", ct);
        if (!response.IsSuccessStatusCode) return null;
        var paged = await response.Content.ReadFromJsonAsync<InvoiceServicePaginatedResponse<InvoiceServiceInvoiceResponse>>(cancellationToken: ct);
        return paged?.Items.FirstOrDefault()?.ToSummary();
    }

    private sealed class InvoiceServicePaginatedResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public PagedResponse<TResult> ToPagedResponse<TResult>(Func<T, TResult> selector)
        {
            var pageSize = PageSize > 0 ? PageSize : Items.Count;
            var totalPages = TotalPages > 0
                ? TotalPages
                : pageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)pageSize) : 0;

            return new PagedResponse<TResult>
            {
                Data = Items.Select(selector).ToList(),
                Meta = new PaginationMeta
                {
                    CurrentPage = Page,
                    PageSize = pageSize,
                    TotalCount = TotalCount,
                    TotalItems = TotalCount,
                    TotalPages = totalPages
                }
            };
        }
    }

    private sealed class InvoiceServiceInvoiceResponse
    {
        public Guid Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public Guid? ParentInvoiceId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public string CustomerTaxId { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public string? ShippingAddress { get; set; }
        public string? QuotationReference { get; set; }
        public string? PoNumber { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal? ExchangeRate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal WithholdingTaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public int PaymentTermsDays { get; set; }
        public DateTime? FinalizedAt { get; set; }
        public string? FinalizedBy { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelledBy { get; set; }
        public string? CancellationReason { get; set; }
        public string? PdfFileReference { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<InvoiceServiceLineResponse> Lines { get; set; } = [];

        public InvoiceSummaryDto ToSummary() => new()
        {
            Id = Id,
            InvoiceNumber = InvoiceNumber ?? string.Empty,
            CustomerName = CustomerName,
            Total = GrandTotal,
            Balance = GrandTotal,
            IssueDate = IssueDate,
            DueDate = DueDate,
            Status = Status,
            CreatedAt = CreatedAt
        };

        public InvoiceDetailDto ToDetail() => new()
        {
            Id = Id,
            InvoiceNumber = InvoiceNumber,
            ParentInvoiceId = ParentInvoiceId,
            CustomerId = CustomerId,
            CustomerName = CustomerName,
            CustomerTaxId = CustomerTaxId,
            BillingAddress = BillingAddress,
            ShippingAddress = ShippingAddress,
            QuotationReference = QuotationReference,
            PoNumber = PoNumber,
            Status = Status,
            Currency = Currency,
            ExchangeRate = ExchangeRate,
            SubTotal = Subtotal,
            TaxAmount = TaxAmount,
            WithholdingTaxAmount = WithholdingTaxAmount,
            Total = GrandTotal,
            IssueDate = IssueDate,
            DueDate = DueDate,
            PaymentTermsDays = PaymentTermsDays,
            FinalizedAt = FinalizedAt,
            FinalizedBy = FinalizedBy,
            CancelledAt = CancelledAt,
            CancelledBy = CancelledBy,
            CancellationReason = CancellationReason,
            PdfFileReference = PdfFileReference,
            Items = Lines.Select(line => new InvoiceItemDto
            {
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                TaxRate = line.TaxRate
            }).ToList(),
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    private sealed class InvoiceServiceLineResponse
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
    }
}
