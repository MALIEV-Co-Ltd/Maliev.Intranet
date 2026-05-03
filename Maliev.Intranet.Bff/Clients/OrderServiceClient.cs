using System.Text.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Order microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class OrderServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of orders.
    /// </summary>
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="search">Optional free-text search term matched against order number and customer name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged response containing order summaries.</returns>
    public async Task<PagedResponse<OrderSummaryDto>?> GetOrdersAsync(Guid? customerId = null, int page = 1, string? search = null, CancellationToken ct = default, int pageSize = 20)
    {
        var url = $"/order/v1/orders?page={page}&pageSize={pageSize}";
        if (customerId.HasValue) url += $"&customerId={customerId.Value}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        var response = await httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var downstream = await response.Content.ReadFromJsonAsync<OrderListResponse>(cancellationToken: ct);
        return downstream?.ToPagedResponse();
    }

    /// <summary>
    /// Retrieves order metrics.
    /// </summary>
    public async Task<int> GetActiveOrderCountAsync(CancellationToken ct = default)
    {
        var httpResponse = await httpClient.GetAsync("/order/v1/metrics/active-count", ct);
        if (!httpResponse.IsSuccessStatusCode) return 0;
        var response = await httpResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return response.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }

    /// <summary>
    /// Retrieves detailed information for a single order by ID.
    /// Returns <c>null</c> when the order is not found (404).
    /// </summary>
    public async Task<OrderDetailDto?> GetOrderByIdAsync(string id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/order/v1/orders/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        var downstream = await response.Content.ReadFromJsonAsync<OrderResponse>(ct);
        return downstream?.ToDetailDto();
    }

    /// <summary>
    /// Updates the status of an order.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateStatusAsync(string id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        return await httpClient.PatchAsJsonAsync($"/order/v1/orders/{id}/status", request, ct);
    }

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateOrderAsync(string id, UpdateOrderRequest request, CancellationToken ct = default)
    {
        return await httpClient.PutAsJsonAsync($"/order/v1/orders/{id}", request, ct);
    }

    /// <summary>
    /// Retrieves the 6-sided preview images stored by OrderService for the given order.
    /// Returns an empty list if the order has no previews yet.
    /// </summary>
    public async Task<List<OrderPreviewImageDto>> GetPreviewImagesAsync(Guid orderId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/order/v1/orders/{orderId}/preview-images", ct);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<OrderPreviewImageDto>>(ct) ?? [];
    }

    /// <summary>
    /// Retrieves the count of orders with status OnHold or Delayed.
    /// </summary>
    public async Task<int> GetOnHoldOrderCountAsync(CancellationToken ct = default)
    {
        var httpResponse = await httpClient.GetAsync("/order/v1/metrics/on-hold-count", ct);
        if (!httpResponse.IsSuccessStatusCode) return 0;
        var response = await httpResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
        return response.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }

    private sealed record OrderListResponse(
        IReadOnlyList<OrderResponse> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages)
    {
        public PagedResponse<OrderSummaryDto> ToPagedResponse()
        {
            return new PagedResponse<OrderSummaryDto>
            {
                Data = Items.Select(item => item.ToSummaryDto()).ToList(),
                Meta = new PaginationMeta
                {
                    CurrentPage = Page,
                    PageSize = PageSize,
                    TotalCount = TotalCount,
                    TotalItems = TotalCount,
                    TotalPages = TotalPages
                }
            };
        }
    }

    private sealed record OrderResponse(
        string OrderId,
        string? CustomerId,
        string? CustomerType,
        string? CurrentStatus,
        int? OrderedQuantity,
        decimal? QuotedAmount,
        string? QuoteCurrency,
        string? ServiceCategoryName,
        string? ProcessTypeName,
        string? Requirements,
        string? CustomerPoNumber,
        Guid? CustomerPoFileId,
        bool IsOutsourced,
        decimal? SupplierCostTHB,
        string? SupplierName,
        DateTime? SupplierEstimatedDelivery,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public OrderSummaryDto ToSummaryDto()
        {
            return new OrderSummaryDto
            {
                Id = Guid.TryParse(OrderId, out var guid) ? guid : Guid.Empty,
                OrderNumber = OrderId,
                CustomerName = CustomerId ?? string.Empty,
                Total = QuotedAmount.GetValueOrDefault(),
                TotalAmount = QuotedAmount.GetValueOrDefault(),
                Status = CurrentStatus ?? string.Empty,
                CreatedAt = CreatedAt,
                IsOutsourced = IsOutsourced
            };
        }

        public OrderDetailDto ToDetailDto()
        {
            return new OrderDetailDto
            {
                Id = Guid.TryParse(OrderId, out var guid) ? guid : Guid.Empty,
                OrderId = OrderId,
                OrderNumber = OrderId,
                CustomerId = Guid.TryParse(CustomerId, out var customerGuid) ? customerGuid : Guid.Empty,
                CustomerName = CustomerId ?? string.Empty,
                CustomerType = CustomerType ?? string.Empty,
                Status = CurrentStatus ?? string.Empty,
                CurrentStatus = CurrentStatus,
                OrderedQuantity = OrderedQuantity,
                TotalAmount = QuotedAmount.GetValueOrDefault(),
                QuotedAmount = QuotedAmount,
                Currency = QuoteCurrency ?? "THB",
                CustomerPoNumber = CustomerPoNumber,
                CustomerPoFileId = CustomerPoFileId,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                IsOutsourced = IsOutsourced,
                SupplierCostTHB = SupplierCostTHB,
                SupplierName = SupplierName,
                SupplierEstimatedDelivery = SupplierEstimatedDelivery,
                Items = BuildItems()
            };
        }

        private List<OrderItemDto> BuildItems()
        {
            var quantity = OrderedQuantity.GetValueOrDefault(1);
            if (quantity <= 0)
            {
                quantity = 1;
            }

            var productName = new[] { ProcessTypeName, ServiceCategoryName, Requirements, OrderId }
                .First(value => !string.IsNullOrWhiteSpace(value))!;

            return
            [
                new OrderItemDto
                {
                    Id = Guid.Empty,
                    Description = productName,
                    ProductCode = ServiceCategoryName,
                    Quantity = quantity,
                    UnitPrice = quantity == 0 ? 0 : QuotedAmount.GetValueOrDefault() / quantity,
                    ServiceType = ProcessTypeName
                }
            ];
        }
    }
}

