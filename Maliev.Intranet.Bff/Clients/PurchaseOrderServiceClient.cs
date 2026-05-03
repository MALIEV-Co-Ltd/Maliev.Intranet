using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the PurchaseOrderService.
/// </summary>
public interface IPurchaseOrderServiceClient
{
    /// <summary>
    /// Retrieves a paged list of purchase orders.
    /// </summary>
    Task<PagedResponse<PurchaseOrderDto>?> GetPurchaseOrdersAsync(
        string? status = null,
        string? orderType = null,
        int? supplierId = null,
        int? orderId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortBy = "createdAt",
        string sortDirection = "desc",
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a purchase order by ID.
    /// </summary>
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new purchase order.
    /// </summary>
    Task<PurchaseOrderDto?> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default);

    /// <summary>
    /// Approves a pending purchase order.
    /// </summary>
    Task<PurchaseOrderDto?> ApprovePurchaseOrderAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Sends an approved purchase order to the supplier.
    /// </summary>
    Task<PurchaseOrderDto?> SendToSupplierAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Marks goods as received for a purchase order.
    /// </summary>
    Task<PurchaseOrderDto?> ReceivePurchaseOrderAsync(int id, bool isPartialReceipt, CancellationToken ct = default);

    /// <summary>
    /// Cancels a purchase order.
    /// </summary>
    Task<bool> CancelPurchaseOrderAsync(int id, CancelPurchaseOrderRequest request, CancellationToken ct = default);

    /// <summary>
    /// Exports a purchase order in the requested format.
    /// </summary>
    Task<HttpResponseMessage> ExportPurchaseOrderAsync(int id, string format = "pdf", CancellationToken ct = default);

    /// <summary>
    /// Registers an uploaded file against a purchase order.
    /// </summary>
    Task<PurchaseOrderFileDto?> RegisterFileAsync(int id, RegisterPurchaseOrderFileRequest request, CancellationToken ct = default);
}

/// <summary>
/// Default implementation of the PurchaseOrderService client.
/// </summary>
public class PurchaseOrderServiceClient(HttpClient httpClient) : IPurchaseOrderServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<PagedResponse<PurchaseOrderDto>?> GetPurchaseOrdersAsync(
        string? status = null,
        string? orderType = null,
        int? supplierId = null,
        int? orderId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortBy = "createdAt",
        string sortDirection = "desc",
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var path = BuildSearchPath(status, orderType, supplierId, orderId, fromDate, toDate, sortBy, sortDirection, page, pageSize);
        using var response = await httpClient.GetAsync(path, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var downstream = await response.Content.ReadFromJsonAsync<DownstreamPaginatedResponse<DownstreamPurchaseOrderResponse>>(JsonOptions, ct);
        return downstream is null ? null : MapPaged(downstream);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int id, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/purchase-order/v1/purchase-orders/{id}", ct);
        return await ReadPurchaseOrderAsync(response, ct);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto?> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default)
    {
        var downstreamRequest = new DownstreamCreatePurchaseOrderRequest
        {
            OrderType = request.OrderType,
            SupplierID = request.SupplierId,
            SupplierServiceId = request.SupplierServiceId,
            OrderID = request.OrderId,
            SourceOrderId = request.SourceOrderId,
            CustomerPO = request.CustomerPo,
            CurrencyID = request.CurrencyId,
            CurrencyServiceId = request.CurrencyServiceId,
            CurrencyCode = request.CurrencyCode,
            WHTRate = request.WhtRate,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Notes = request.Notes,
            Items = request.Items
                .Where(item => (item.ExternalOrderItemId > 0 || !string.IsNullOrWhiteSpace(item.SourceOrderItemId)) && item.Quantity > 0)
                .Select(item => new DownstreamPartialOrderItemRequest(item.ExternalOrderItemId, item.SourceOrderItemId, item.Quantity))
                .ToList()
        };

        using var response = await httpClient.PostAsJsonAsync("/purchase-order/v1/purchase-orders", downstreamRequest, JsonOptions, ct);
        return await ReadPurchaseOrderAsync(response, ct);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto?> ApprovePurchaseOrderAsync(int id, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsync($"/purchase-order/v1/purchase-orders/{id}/approve", null, ct);
        return await ReadPurchaseOrderAsync(response, ct);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto?> SendToSupplierAsync(int id, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsync($"/purchase-order/v1/purchase-orders/{id}/send-to-supplier", null, ct);
        return await ReadPurchaseOrderAsync(response, ct);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto?> ReceivePurchaseOrderAsync(int id, bool isPartialReceipt, CancellationToken ct = default)
    {
        var value = isPartialReceipt ? "true" : "false";
        using var response = await httpClient.PostAsync($"/purchase-order/v1/purchase-orders/{id}/receive?isPartialReceipt={value}", null, ct);
        return await ReadPurchaseOrderAsync(response, ct);
    }

    /// <inheritdoc />
    public async Task<bool> CancelPurchaseOrderAsync(int id, CancelPurchaseOrderRequest request, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/purchase-order/v1/purchase-orders/{id}/cancel",
            new DownstreamCancelPurchaseOrderRequest(request.Reason),
            JsonOptions,
            ct);

        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportPurchaseOrderAsync(int id, string format = "pdf", CancellationToken ct = default)
    {
        var safeFormat = string.IsNullOrWhiteSpace(format) ? "pdf" : format;
        return httpClient.GetAsync($"/purchase-order/v1/purchase-orders/{id}/export?format={Uri.EscapeDataString(safeFormat)}", ct);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderFileDto?> RegisterFileAsync(int id, RegisterPurchaseOrderFileRequest request, CancellationToken ct = default)
    {
        var downstreamRequest = new DownstreamRegisterPurchaseOrderFileRequest
        {
            FileName = request.FileName,
            ObjectName = request.ObjectName,
            FileSize = request.FileSize,
            ContentType = request.ContentType,
            DocumentType = MapDocumentType(request.DocumentType),
            Description = request.Description
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"/purchase-order/v1/purchase-orders/{id}/files",
            downstreamRequest,
            JsonOptions,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var file = await response.Content.ReadFromJsonAsync<DownstreamPurchaseOrderFileResponse>(JsonOptions, ct);
        return file?.ToDto();
    }

    private static string BuildSearchPath(
        string? status,
        string? orderType,
        int? supplierId,
        int? orderId,
        DateTime? fromDate,
        DateTime? toDate,
        string sortBy,
        string sortDirection,
        int page,
        int pageSize)
    {
        var query = new List<string>
        {
            $"Page={Math.Max(1, page).ToString(CultureInfo.InvariantCulture)}",
            $"PageSize={Math.Clamp(pageSize, 1, 100).ToString(CultureInfo.InvariantCulture)}"
        };

        AddIfPresent(query, "Status", status);
        AddIfPresent(query, "OrderType", orderType);
        AddIfPresent(query, "SupplierId", supplierId?.ToString(CultureInfo.InvariantCulture));
        AddIfPresent(query, "OrderId", orderId?.ToString(CultureInfo.InvariantCulture));
        AddIfPresent(query, "FromDate", fromDate?.ToString("O", CultureInfo.InvariantCulture));
        AddIfPresent(query, "ToDate", toDate?.ToString("O", CultureInfo.InvariantCulture));
        AddIfPresent(query, "SortBy", sortBy);
        AddIfPresent(query, "SortDirection", sortDirection);

        return "/purchase-order/v1/purchase-orders?" + string.Join('&', query);
    }

    private static void AddIfPresent(List<string> query, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Add($"{key}={Uri.EscapeDataString(value)}");
        }
    }

    private static async Task<PurchaseOrderDto?> ReadPurchaseOrderAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var downstream = await response.Content.ReadFromJsonAsync<DownstreamPurchaseOrderDetailResponse>(JsonOptions, ct);
        return downstream?.ToDto();
    }

    private static PagedResponse<PurchaseOrderDto> MapPaged(DownstreamPaginatedResponse<DownstreamPurchaseOrderResponse> response)
    {
        var totalPages = response.PageSize <= 0 ? 0 : (int)Math.Ceiling(response.TotalCount / (double)response.PageSize);
        return new PagedResponse<PurchaseOrderDto>
        {
            Data = response.Items.Select(item => item.ToDto()).ToList(),
            Meta = new PaginationMeta
            {
                CurrentPage = response.Page,
                PageSize = response.PageSize,
                TotalCount = response.TotalCount,
                TotalItems = response.TotalCount,
                TotalPages = totalPages
            }
        };
    }

    private sealed class DownstreamPaginatedResponse<T>
    {
        public List<T> Items { get; set; } = [];

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    private class DownstreamPurchaseOrderResponse
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public string OrderType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int SupplierID { get; set; }

        public Guid? SupplierServiceId { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public int OrderID { get; set; }

        public string? SourceOrderId { get; set; }

        public string CurrencyCode { get; set; } = "THB";

        public decimal TotalAmount { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual PurchaseOrderDto ToDto() => new()
        {
            Id = Id,
            PoNumber = OrderNumber,
            OrderType = OrderType,
            Status = Status,
            SupplierId = SupplierID,
            SupplierServiceId = SupplierServiceId,
            SupplierName = SupplierName,
            OrderId = OrderID,
            SourceOrderId = SourceOrderId,
            CurrencyCode = CurrencyCode,
            TotalAmount = TotalAmount,
            Date = CreatedAt,
            CreatedDate = CreatedAt,
            ExpectedDelivery = ExpectedDeliveryDate
        };
    }

    private sealed class DownstreamPurchaseOrderDetailResponse : DownstreamPurchaseOrderResponse
    {
        public string? SupplierContactInfo { get; set; }

        public string? CustomerPO { get; set; }

        public int CurrencyID { get; set; }

        public Guid? CurrencyServiceId { get; set; }

        public string? CurrencySymbol { get; set; }

        public DateTime? OrderDate { get; set; }

        public decimal SubtotalAmount { get; set; }

        public decimal? WHTRate { get; set; }

        public decimal? WHTAmount { get; set; }

        public string? Notes { get; set; }

        public List<DownstreamOrderItemResponse> Items { get; set; } = [];

        public List<DownstreamPurchaseOrderFileResponse> Files { get; set; } = [];

        public string? CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        public DateTime? LastModifiedAt { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string RowVersion { get; set; } = string.Empty;

        public override PurchaseOrderDto ToDto()
        {
            var dto = base.ToDto();
            dto.CustomerPo = CustomerPO;
            dto.CurrencyId = CurrencyID;
            dto.CurrencyServiceId = CurrencyServiceId;
            dto.CurrencySymbol = CurrencySymbol;
            dto.Date = OrderDate ?? dto.Date;
            dto.SubtotalAmount = SubtotalAmount;
            dto.WhtRate = WHTRate;
            dto.WhtAmount = WHTAmount;
            dto.SupplierContactInfo = SupplierContactInfo;
            dto.Notes = Notes;
            dto.RowVersion = RowVersion;
            dto.CreatedBy = CreatedBy;
            dto.LastModifiedBy = LastModifiedBy;
            dto.LastModifiedAt = LastModifiedAt;
            dto.ApprovedBy = ApprovedBy;
            dto.ApprovedAt = ApprovedAt;
            dto.Items = Items.Select(item => item.ToDto()).ToList();
            dto.Files = Files.Select(file => file.ToDto()).ToList();
            return dto;
        }
    }

    private sealed class DownstreamOrderItemResponse
    {
        public int Id { get; set; }

        public int ExternalOrderItemId { get; set; }

        public string? SourceOrderItemId { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string UnitOfMeasure { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public string Currency { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public DateTime CachedAt { get; set; }

        public bool ExternallyModified { get; set; }

        public PurchaseOrderLineItemDto ToDto() => new()
        {
            Id = Id,
            ExternalOrderItemId = ExternalOrderItemId,
            SourceOrderItemId = SourceOrderItemId,
            ProductCode = ProductCode,
            ProductName = ProductName,
            Quantity = Quantity,
            UnitOfMeasure = UnitOfMeasure,
            UnitPrice = UnitPrice,
            TotalPrice = TotalPrice,
            Currency = Currency,
            Notes = Notes,
            CachedAt = CachedAt,
            ExternallyModified = ExternallyModified
        };
    }

    private sealed class DownstreamPurchaseOrderFileResponse
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string ObjectName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public JsonElement DocumentType { get; set; }

        public DateTime UploadedAt { get; set; }

        public string UploadedBy { get; set; } = string.Empty;

        public string? Description { get; set; }

        public PurchaseOrderFileDto ToDto() => new()
        {
            Id = Id,
            PurchaseOrderId = PurchaseOrderId,
            FileName = FileName,
            ObjectName = ObjectName,
            FileSize = FileSize,
            ContentType = ContentType,
            DocumentType = FormatDocumentType(DocumentType),
            UploadedAt = UploadedAt,
            UploadedBy = UploadedBy,
            Description = Description
        };
    }

    private sealed class DownstreamCreatePurchaseOrderRequest
    {
        [JsonPropertyName("orderType")]
        public int OrderType { get; set; }

        [JsonPropertyName("supplierID")]
        public int SupplierID { get; set; }

        [JsonPropertyName("supplierServiceId")]
        public Guid? SupplierServiceId { get; set; }

        [JsonPropertyName("orderID")]
        public int OrderID { get; set; }

        [JsonPropertyName("sourceOrderId")]
        public string? SourceOrderId { get; set; }

        [JsonPropertyName("customerPO")]
        public string? CustomerPO { get; set; }

        [JsonPropertyName("currencyID")]
        public int CurrencyID { get; set; }

        [JsonPropertyName("currencyServiceId")]
        public Guid? CurrencyServiceId { get; set; }

        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("whtRate")]
        public decimal WHTRate { get; set; }

        [JsonPropertyName("expectedDeliveryDate")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("items")]
        public List<DownstreamPartialOrderItemRequest> Items { get; set; } = [];
    }

    private sealed record DownstreamPartialOrderItemRequest(
        [property: JsonPropertyName("externalOrderItemId")] int ExternalOrderItemId,
        [property: JsonPropertyName("sourceOrderItemId")] string? SourceOrderItemId,
        [property: JsonPropertyName("quantity")] decimal Quantity);

    private sealed record DownstreamCancelPurchaseOrderRequest(
        [property: JsonPropertyName("reason")] string Reason);

    private sealed class DownstreamRegisterPurchaseOrderFileRequest
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("objectName")]
        public string ObjectName { get; set; } = string.Empty;

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("documentType")]
        public int DocumentType { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private static int MapDocumentType(string? documentType)
    {
        return documentType?.Trim().ToLowerInvariant() switch
        {
            "customerpo" or "customer po" => 0,
            "internalapproval" or "internal approval" => 1,
            "invoice" => 2,
            "reference" => 3,
            "generatedpdf" or "generated pdf" => 4,
            "other" => 5,
            _ => 3
        };
    }

    private static string FormatDocumentType(JsonElement documentType)
    {
        return documentType.ValueKind switch
        {
            JsonValueKind.String => documentType.GetString() ?? string.Empty,
            JsonValueKind.Number when documentType.TryGetInt32(out var value) => value switch
            {
                0 => "CustomerPO",
                1 => "InternalApproval",
                2 => "Invoice",
                3 => "Reference",
                4 => "GeneratedPDF",
                5 => "Other",
                _ => value.ToString(CultureInfo.InvariantCulture)
            },
            _ => string.Empty
        };
    }
}
