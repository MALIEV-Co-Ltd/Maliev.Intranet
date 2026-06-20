using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client interface for interacting with the Delivery microservice.
/// </summary>
public interface IDeliveryServiceClient
{
    /// <summary>
    /// Retrieves a paged list of delivery notes.
    /// </summary>
    Task<PagedResponse<DeliveryNoteSummaryDto>?> GetDeliveryNotesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a delivery note by ID.
    /// </summary>
    Task<DeliveryNoteDetailDto?> GetDeliveryNoteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new delivery note.
    /// </summary>
    Task<DeliveryNoteDetailDto?> CreateDeliveryNoteAsync(CreateDeliveryNoteRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a delivery note.
    /// </summary>
    Task<DeliveryNoteDetailDto?> UpdateDeliveryStatusAsync(string id, UpdateDeliveryStatusRequest request, CancellationToken ct = default);

    /// <summary>
    /// Generates a PDF for a delivery note.
    /// </summary>
    Task<DeliveryPdfRequestResponse?> GeneratePdfAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves files attached to a delivery note.
    /// </summary>
    Task<List<DeliveryNoteFileDto>> GetFilesAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Downloads a file attached to a delivery note.
    /// </summary>
    Task<HttpResponseMessage> DownloadFileAsync(string id, Guid fileId, CancellationToken ct = default);

    /// <summary>
    /// Uploads a proof or document file to a delivery note.
    /// </summary>
    Task<DeliveryNoteFileDto?> UploadFileAsync(string id, Stream content, string fileName, string contentType, string fileType, string? description, CancellationToken ct = default);

    /// <summary>
    /// Deletes a delivery note.
    /// </summary>
    Task<bool> DeleteDeliveryNoteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Gets available shipping couriers from DeliveryService.
    /// </summary>
    Task<List<ShippingCourierDto>> GetShippingCouriersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets live shipping rates from DeliveryService.
    /// </summary>
    Task<ShippingRateResponseDto> GetShippingRatesAsync(ShippingRateRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Gets tracking status from DeliveryService.
    /// </summary>
    Task<ShippingTrackingDto?> GetShippingTrackingAsync(string trackingCode, CancellationToken ct = default);
}

/// <summary>
/// Client implementation for interacting with the Delivery microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class DeliveryServiceClient(HttpClient httpClient) : IDeliveryServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<PagedResponse<DeliveryNoteSummaryDto>?> GetDeliveryNotesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<DownstreamPagedResponse<DeliveryNoteSummaryDto>>(
            $"/delivery/v1/delivery-notes?page={page}&pageSize={pageSize}&sortBy=delivery_date&sortOrder=desc",
            JsonOptions,
            ct);

        return new PagedResponse<DeliveryNoteSummaryDto>
        {
            Data = result?.Items.Select(NormalizeSummary).ToList() ?? [],
            Meta = new PaginationMeta
            {
                CurrentPage = result?.Page ?? page,
                PageSize = result?.PageSize ?? pageSize,
                TotalCount = result?.TotalCount ?? 0,
                TotalItems = result?.TotalCount ?? 0,
                TotalPages = result?.TotalPages > 0
                    ? result.TotalPages
                    : CalculateTotalPages(result?.TotalCount ?? 0, result?.PageSize ?? pageSize)
            }
        };
    }

    /// <inheritdoc />
    public async Task<DeliveryNoteDetailDto?> GetDeliveryNoteAsync(string id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/delivery/v1/delivery-notes/{Uri.EscapeDataString(id)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<DeliveryNoteDetailDto>(JsonOptions, ct);
        return result is null ? null : NormalizeDetail(result);
    }

    /// <inheritdoc />
    public async Task<DeliveryNoteDetailDto?> CreateDeliveryNoteAsync(CreateDeliveryNoteRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/delivery/v1/delivery-notes", ToDownstreamCreateRequest(request), JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<DeliveryNoteDetailDto>(JsonOptions, ct);
        return result is null ? null : NormalizeDetail(result);
    }

    /// <inheritdoc />
    public async Task<DeliveryNoteDetailDto?> UpdateDeliveryStatusAsync(string id, UpdateDeliveryStatusRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync(
            $"/delivery/v1/delivery-notes/{Uri.EscapeDataString(id)}/status",
            ToDownstreamStatusRequest(request),
            JsonOptions,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<DeliveryNoteDetailDto>(JsonOptions, ct);
        return result is null ? null : NormalizeDetail(result);
    }

    /// <inheritdoc />
    public async Task<DeliveryPdfRequestResponse?> GeneratePdfAsync(string id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/delivery/v1/delivery-notes/{Uri.EscapeDataString(id)}/generate-pdf", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DeliveryPdfRequestResponse>(JsonOptions, ct);
    }

    /// <inheritdoc />
    public async Task<List<DeliveryNoteFileDto>> GetFilesAsync(string id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/delivery/v1/delivery-notes/{Uri.EscapeDataString(id)}/files", ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<DeliveryNoteFileDto>>(JsonOptions, ct) ?? [];
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> DownloadFileAsync(string id, Guid fileId, CancellationToken ct = default)
        => await httpClient.GetAsync($"/delivery/v1/delivery-notes/{Uri.EscapeDataString(id)}/files/{fileId:D}/download", ct);

    /// <inheritdoc />
    public async Task<DeliveryNoteFileDto?> UploadFileAsync(
        string id,
        Stream content,
        string fileName,
        string contentType,
        string fileType,
        string? description,
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(fileType), "fileType");
        if (!string.IsNullOrWhiteSpace(description))
        {
            form.Add(new StringContent(description), "description");
        }

        var response = await httpClient.PostAsync($"/delivery/v1/delivery-notes/{Uri.EscapeDataString(id)}/files", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DeliveryNoteFileDto>(JsonOptions, ct);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDeliveryNoteAsync(string id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/delivery/v1/delivery-notes/{Uri.EscapeDataString(id)}", ct);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<List<ShippingCourierDto>> GetShippingCouriersAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/delivery/v1/shipping/couriers", ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<ShippingCourierDto>>(JsonOptions, ct) ?? [];
    }

    /// <inheritdoc />
    public async Task<ShippingRateResponseDto> GetShippingRatesAsync(ShippingRateRequestDto request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/delivery/v1/shipping/rates", request, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            return new ShippingRateResponseDto();
        }

        var downstreamRates = await response.Content.ReadFromJsonAsync<List<DownstreamShippingRateOption>>(JsonOptions, ct) ?? [];
        return new ShippingRateResponseDto
        {
            Rates = downstreamRates.Select(rate => new ShippingRateOptionDto
            {
                CourierCode = rate.CourierCode,
                ProductName = FirstNonEmpty(rate.CourierName, rate.CourierCode),
                TotalPrice = rate.Price,
                CurrencyCode = FirstNonEmpty(rate.Currency, "THB"),
                EstimatedDeliveryDate = rate.EstimatedDelivery,
                ServiceLevel = rate.ServiceLevel,
                Provider = rate.Provider
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<ShippingTrackingDto?> GetShippingTrackingAsync(string trackingCode, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/delivery/v1/shipping/tracking/{Uri.EscapeDataString(trackingCode)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ShippingTrackingDto>(JsonOptions, ct);
    }

    private static object ToDownstreamCreateRequest(CreateDeliveryNoteRequest request) => new
    {
        orderId = NullIfWhiteSpace(request.OrderId),
        purchaseOrderId = request.PurchaseOrderId,
        customerId = request.CustomerId,
        customerName = NullIfWhiteSpace(request.CustomerName),
        deliveryDate = request.DeliveryDate,
        shippingAddressLine1 = NullIfWhiteSpace(request.ShippingAddressLine1),
        shippingAddressLine2 = NullIfWhiteSpace(request.ShippingAddressLine2),
        shippingCity = NullIfWhiteSpace(request.ShippingCity),
        shippingProvince = NullIfWhiteSpace(request.ShippingProvince),
        shippingPostalCode = NullIfWhiteSpace(request.ShippingPostalCode),
        shippingCountry = NullIfWhiteSpace(request.ShippingCountry),
        deliveryContactName = NullIfWhiteSpace(request.DeliveryContactName),
        deliveryContactPhone = NullIfWhiteSpace(request.DeliveryContactPhone),
        deliveryContactEmail = NullIfWhiteSpace(request.DeliveryContactEmail),
        carrierName = NullIfWhiteSpace(request.CarrierName),
        trackingNumber = NullIfWhiteSpace(request.TrackingNumber),
        shippingCost = request.ShippingCost,
        shippingCostCurrency = NullIfWhiteSpace(request.ShippingCostCurrency),
        deliveryInstructions = NullIfWhiteSpace(request.DeliveryInstructions),
        items = request.Items.Select(item => new
        {
            orderId = NullIfWhiteSpace(item.OrderId ?? request.OrderId),
            purchaseOrderItemId = item.PurchaseOrderItemId,
            productCode = item.ProductCode,
            productName = item.ProductName,
            productDescription = NullIfWhiteSpace(item.ProductDescription),
            quantityOrdered = item.QuantityOrdered,
            quantityManufactured = item.QuantityManufactured,
            quantityDelivered = item.QuantityDelivered,
            unitOfMeasure = item.UnitOfMeasure,
            itemNotes = NullIfWhiteSpace(item.ItemNotes)
        }).ToList()
    };

    private static object ToDownstreamStatusRequest(UpdateDeliveryStatusRequest request) => new
    {
        newStatus = request.NewStatus,
        actualDeliveryTime = request.ActualDeliveryTime,
        receivedByName = NullIfWhiteSpace(request.ReceivedByName),
        signatureFileId = request.SignatureFileId
    };

    private static DeliveryNoteSummaryDto NormalizeSummary(DeliveryNoteSummaryDto summary)
    {
        var id = FirstNonEmpty(summary.DeliveryNoteId, summary.Id, summary.DeliveryNoteNumber);
        summary.Id = id;
        summary.DeliveryNoteId = id;
        summary.DeliveryNoteNumber = FirstNonEmpty(summary.DeliveryNoteNumber, id);
        summary.OrderNumber = FirstNonEmpty(summary.OrderNumber, summary.OrderId, "-");
        return summary;
    }

    private static DeliveryNoteDetailDto NormalizeDetail(DeliveryNoteDetailDto detail)
    {
        var id = FirstNonEmpty(detail.DeliveryNoteId, detail.Id, detail.DeliveryNoteNumber);
        detail.Id = id;
        detail.DeliveryNoteId = id;
        detail.DeliveryNoteNumber = FirstNonEmpty(detail.DeliveryNoteNumber, id);
        detail.OrderNumber = FirstNonEmpty(detail.OrderNumber, detail.OrderId, "-");
        detail.CustomerAddress = BuildAddress(detail);
        return detail;
    }

    private static string BuildAddress(DeliveryNoteDetailDto detail)
    {
        var parts = new[]
        {
            detail.ShippingAddressLine1,
            detail.ShippingAddressLine2,
            detail.ShippingCity,
            detail.ShippingProvince,
            detail.ShippingPostalCode,
            detail.ShippingCountry
        };

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static int CalculateTotalPages(int totalCount, int pageSize) =>
        pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed record DownstreamPagedResponse<T>
    {
        public List<T> Items { get; init; } = [];

        public int TotalCount { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }

        public int TotalPages { get; init; }
    }

    private sealed record DownstreamShippingRateOption
    {
        public string CourierCode { get; init; } = string.Empty;

        public string CourierName { get; init; } = string.Empty;

        public decimal Price { get; init; }

        public string Currency { get; init; } = "THB";

        public string? ServiceLevel { get; init; }

        public string? EstimatedDelivery { get; init; }

        public string Provider { get; init; } = string.Empty;
    }
}
