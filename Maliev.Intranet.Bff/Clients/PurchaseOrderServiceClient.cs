using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Purchase Order microservice.
/// </summary>
public interface IPurchaseOrderServiceClient
{
    /// <summary>
    /// Retrieves a paged list of purchase orders.
    /// </summary>
    Task<PagedResponse<PurchaseOrderDto>?> GetPurchaseOrdersAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a purchase order by ID.
    /// </summary>
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new purchase order.
    /// </summary>
    Task<PurchaseOrderDto?> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default);
}

/// <summary>
/// Default implementation of the purchase order service client.
/// </summary>
public class PurchaseOrderServiceClient(HttpClient httpClient) : IPurchaseOrderServiceClient
{
    /// <inheritdoc />
    public async Task<PagedResponse<PurchaseOrderDto>?> GetPurchaseOrdersAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<PurchaseOrderDto>>($"/purchase-order/v1/orders?page={page}&pageSize={pageSize}", ct);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PurchaseOrderDto>($"/purchase-order/v1/orders/{id}", ct);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto?> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/purchase-order/v1/orders", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<PurchaseOrderDto>(cancellationToken: ct);
        }
        return null;
    }
}
