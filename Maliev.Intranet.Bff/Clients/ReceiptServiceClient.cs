using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Receipt microservice.
/// </summary>
public interface IReceiptServiceClient
{
    /// <summary>
    /// Retrieves a paged list of receipts.
    /// </summary>
    Task<PagedResponse<ReceiptDto>?> GetReceiptsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a receipt by ID.
    /// </summary>
    Task<ReceiptDto?> GetReceiptByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new receipt.
    /// </summary>
    Task<ReceiptDto?> CreateReceiptAsync(CreateReceiptRequest request, CancellationToken ct = default);

    /// <summary>
    /// Voids a receipt.
    /// </summary>
    Task<bool> VoidReceiptAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Default implementation of the receipt service client.
/// </summary>
public class ReceiptServiceClient(HttpClient httpClient) : IReceiptServiceClient
{
    /// <inheritdoc />
    public async Task<PagedResponse<ReceiptDto>?> GetReceiptsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<ReceiptServicePagedResponse<ReceiptServiceReceiptResponse>>($"/receipt/v1/receipts?page={page}&pageSize={pageSize}", ct);
        return response?.ToPagedResponse() ?? new PagedResponse<ReceiptDto>();
    }

    /// <inheritdoc />
    public async Task<ReceiptDto?> GetReceiptByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<ReceiptServiceReceiptResponse>($"/receipt/v1/receipts/{id}", ct);
        return response?.ToDto();
    }

    /// <inheritdoc />
    public async Task<ReceiptDto?> CreateReceiptAsync(CreateReceiptRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/receipt/v1/receipts", request, ct);
        if (response.IsSuccessStatusCode)
        {
            var receipt = await response.Content.ReadFromJsonAsync<ReceiptServiceReceiptResponse>(cancellationToken: ct);
            return receipt?.ToDto();
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> VoidReceiptAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/receipt/v1/receipts/{id}/void", null, ct);
        return response.IsSuccessStatusCode;
    }

    private sealed class ReceiptServicePagedResponse<T>
    {
        public List<T> Data { get; set; } = [];
        public ReceiptServicePaginationMetadata Pagination { get; set; } = new();

        public PagedResponse<ReceiptDto> ToPagedResponse()
        {
            return new PagedResponse<ReceiptDto>
            {
                Data = Data.OfType<ReceiptServiceReceiptResponse>().Select(receipt => receipt.ToDto()).ToList(),
                Meta = new PaginationMeta
                {
                    CurrentPage = Pagination.CurrentPage,
                    PageSize = Pagination.PageSize,
                    TotalCount = Pagination.TotalCount,
                    TotalItems = Pagination.TotalCount,
                    TotalPages = Pagination.TotalPages
                }
            };
        }
    }

    private sealed class ReceiptServicePaginationMetadata
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class ReceiptServiceReceiptResponse
    {
        public Guid Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public Guid InvoiceId { get; set; }
        public DateTime IssueDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? PdfReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<ReceiptServiceLineItemResponse> LineItems { get; set; } = [];

        public ReceiptDto ToDto() => new()
        {
            Id = Id,
            ReceiptNumber = ReceiptNumber,
            InvoiceId = InvoiceId,
            CustomerName = CustomerName,
            Date = IssueDate,
            IssueDate = IssueDate,
            TotalAmount = TotalAmount,
            PaymentMethod = PaymentMethod ?? "Bank Transfer",
            Status = Status,
            PdfReferenceId = PdfReferenceId,
            CreatedAt = CreatedAt,
            CreatedBy = CreatedBy,
            Lines = LineItems.Select(line => new ReceiptLineItemDto
            {
                Description = line.Description,
                Amount = line.LineTotal
            }).ToList()
        };
    }

    private sealed class ReceiptServiceLineItemResponse
    {
        public string Description { get; set; } = string.Empty;
        public decimal LineTotal { get; set; }
    }
}
