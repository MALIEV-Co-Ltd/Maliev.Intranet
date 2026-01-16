using System.Net.Http.Json;
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
}
