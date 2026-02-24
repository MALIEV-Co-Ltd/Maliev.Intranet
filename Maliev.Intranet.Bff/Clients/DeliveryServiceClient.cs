using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

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
    Task<DeliveryNoteDetailDto?> GetDeliveryNoteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new delivery note.
    /// </summary>
    Task<DeliveryNoteDetailDto?> CreateDeliveryNoteAsync(CreateDeliveryNoteRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a delivery note.
    /// </summary>
    Task<DeliveryNoteDetailDto?> UpdateDeliveryStatusAsync(Guid id, UpdateDeliveryStatusRequest request, CancellationToken ct = default);

    /// <summary>
    /// Generates a PDF for a delivery note.
    /// </summary>
    Task<string?> GeneratePdfAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Deletes a delivery note.
    /// </summary>
    Task<bool> DeleteDeliveryNoteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Client implementation for interacting with the Delivery microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class DeliveryServiceClient(HttpClient httpClient) : IDeliveryServiceClient
{
    /// <inheritdoc />
    public async Task<PagedResponse<DeliveryNoteSummaryDto>?> GetDeliveryNotesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<DeliveryNoteSummaryDto>>($"/delivery/v1/delivery-notes?page={page}&pageSize={pageSize}", ct);
    }

    /// <inheritdoc />
    public async Task<DeliveryNoteDetailDto?> GetDeliveryNoteAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<DeliveryNoteDetailDto>($"/delivery/v1/delivery-notes/{id}", ct);
    }

    /// <inheritdoc />
    public async Task<DeliveryNoteDetailDto?> CreateDeliveryNoteAsync(CreateDeliveryNoteRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/delivery/v1/delivery-notes", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<DeliveryNoteDetailDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<DeliveryNoteDetailDto?> UpdateDeliveryStatusAsync(Guid id, UpdateDeliveryStatusRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/delivery/v1/delivery-notes/{id}/status", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<DeliveryNoteDetailDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<string?> GeneratePdfAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/delivery/v1/delivery-notes/{id}/generate-pdf", null, ct);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            return result.GetProperty("pdfUrl").GetString();
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDeliveryNoteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/delivery/v1/delivery-notes/{id}", ct);
        return response.IsSuccessStatusCode;
    }
}
