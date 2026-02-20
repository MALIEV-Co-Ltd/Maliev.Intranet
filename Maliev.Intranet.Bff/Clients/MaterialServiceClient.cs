using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Material microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class MaterialServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of materials.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing material summaries.</returns>
    public async Task<PagedResponse<MaterialSummaryDto>?> GetMaterialsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<MaterialSummaryDto>>($"/material/v1/materials?page={page}&pageSize={pageSize}", ct);
    }

    /// <summary>
    /// Retrieves detailed information for a single material by ID.
    /// </summary>
    /// <param name="id">The material ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The material detail DTO.</returns>
    public async Task<MaterialDetailDto?> GetMaterialByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<MaterialDetailDto>($"/material/v1/materials/{id}", ct);
    }

    /// <summary>
    /// Creates a new material.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created material summary.</returns>
    public async Task<MaterialSummaryDto?> CreateMaterialAsync(CreateMaterialRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/material/v1/materials", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MaterialSummaryDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Updates an existing material.
    /// </summary>
    /// <param name="id">The material ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated material detail.</returns>
    public async Task<MaterialDetailDto?> UpdateMaterialAsync(Guid id, UpdateMaterialRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/material/v1/materials/{id}", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MaterialDetailDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Deletes a material.
    /// </summary>
    /// <param name="id">The material ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> DeleteMaterialAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/material/v1/materials/{id}", ct);
        return response.IsSuccessStatusCode;
    }
}
