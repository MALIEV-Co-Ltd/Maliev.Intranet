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
        return await httpClient.GetFromJsonAsync<PagedResponse<ReceiptDto>>($"/receipt/v1/receipts?page={page}&pageSize={pageSize}", ct);
    }

    /// <inheritdoc />
    public async Task<ReceiptDto?> GetReceiptByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<ReceiptDto>($"/receipt/v1/receipts/{id}", ct);
    }

    /// <inheritdoc />
    public async Task<ReceiptDto?> CreateReceiptAsync(CreateReceiptRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/receipt/v1/receipts", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ReceiptDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> VoidReceiptAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/receipt/v1/receipts/{id}/void", null, ct);
        return response.IsSuccessStatusCode;
    }
}
