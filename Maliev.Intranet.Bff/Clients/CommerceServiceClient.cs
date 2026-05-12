using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.WebUtilities;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Commerce microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public sealed class CommerceServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Lists products for employee catalog management, including drafts and archived records.
    /// </summary>
    public async Task<PagedResponse<CommerceProductSummaryDto>> ListManagedProductsAsync(
        string? query,
        string? collection,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var path = QueryHelpers.AddQueryString("/commerce/v1/products/manage", new Dictionary<string, string?>
        {
            ["query"] = query,
            ["collection"] = collection,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        });

        var response = await httpClient.GetFromJsonAsync<CommerceServicePagedResponse<CommerceProductSummaryDto>>(path, cancellationToken);
        return response is null
            ? new PagedResponse<CommerceProductSummaryDto>()
            : new PagedResponse<CommerceProductSummaryDto>
            {
                Data = response.Items,
                Meta = new PaginationMeta
                {
                    CurrentPage = response.Page,
                    PageSize = response.PageSize,
                    TotalItems = response.TotalCount,
                    TotalCount = response.TotalCount,
                    TotalPages = response.PageSize <= 0 ? 0 : (int)Math.Ceiling(response.TotalCount / (double)response.PageSize)
                }
            };
    }

    /// <summary>
    /// Gets a managed product by handle.
    /// </summary>
    public async Task<CommerceProductDto?> GetManagedProductAsync(string handle, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<CommerceProductDto>($"/commerce/v1/products/manage/{Uri.EscapeDataString(handle)}", cancellationToken);
    }

    /// <summary>
    /// Creates a product listing.
    /// </summary>
    public async Task<CommerceProductDto?> CreateProductAsync(CommerceProductMutationRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/commerce/v1/products", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommerceProductDto>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates a product listing.
    /// </summary>
    public async Task<CommerceProductDto?> UpdateProductAsync(Guid id, CommerceProductMutationRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PatchAsJsonAsync($"/commerce/v1/products/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommerceProductDto>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates a product publishing status.
    /// </summary>
    public async Task<CommerceProductDto?> UpdateProductStatusAsync(Guid id, CommerceProductStatusRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PatchAsJsonAsync($"/commerce/v1/products/{id}/status", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommerceProductDto>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Archives a product listing.
    /// </summary>
    public async Task<bool> ArchiveProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"/commerce/v1/products/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Lists product collections for employee management.
    /// </summary>
    public async Task<List<CommerceCollectionDto>> ListManagedCollectionsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<CommerceCollectionDto>>("/commerce/v1/collections/manage", cancellationToken) ?? [];
    }

    /// <summary>
    /// Creates a product collection.
    /// </summary>
    public async Task<CommerceCollectionDto?> CreateCollectionAsync(CommerceCollectionMutationRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/commerce/v1/collections", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommerceCollectionDto>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates a product collection.
    /// </summary>
    public async Task<CommerceCollectionDto?> UpdateCollectionAsync(Guid id, CommerceCollectionMutationRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"/commerce/v1/collections/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommerceCollectionDto>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Unpublishes a product collection.
    /// </summary>
    public async Task<bool> UnpublishCollectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"/commerce/v1/collections/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private sealed class CommerceServicePagedResponse<T>
    {
        public List<T> Items { get; set; } = [];

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }
    }
}
