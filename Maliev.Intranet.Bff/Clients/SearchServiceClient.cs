using System.Net.Http.Json;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for querying the SearchService global index.
/// </summary>
/// <param name="httpClient">Configured HTTP client for SearchService.</param>
public class SearchServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Searches the global index.
    /// </summary>
    /// <param name="query">Search query text.</param>
    /// <param name="limit">Maximum number of rows to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>SearchService response, or <c>null</c> if the service returned no body.</returns>
    public virtual async Task<SearchServiceResponseDto?> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        var url = $"/search/v1/search?query={Uri.EscapeDataString(query)}&limit={limit}";
        return await httpClient.GetFromJsonAsync<SearchServiceResponseDto>(url, ct);
    }
}

/// <summary>
/// Response envelope returned by SearchService.
/// </summary>
/// <param name="Query">Normalized query text.</param>
/// <param name="TotalCount">Total result count.</param>
/// <param name="Results">Search results visible to the caller.</param>
public record SearchServiceResponseDto(
    string Query,
    int TotalCount,
    IReadOnlyList<SearchServiceResultDto> Results);

/// <summary>
/// Search result returned by SearchService.
/// </summary>
/// <param name="SourceService">Service that owns the indexed document.</param>
/// <param name="ResourceType">Business resource type.</param>
/// <param name="ResourceId">Stable source resource identifier.</param>
/// <param name="Title">Primary result text.</param>
/// <param name="Subtitle">Secondary result text.</param>
/// <param name="Summary">Longer result summary.</param>
/// <param name="Status">Optional business status.</param>
/// <param name="RequiredPermission">Permission required by SearchService.</param>
/// <param name="Score">Search relevance score.</param>
/// <param name="UpdatedAtUtc">UTC timestamp when the source document last changed.</param>
public record SearchServiceResultDto(
    string SourceService,
    string ResourceType,
    string ResourceId,
    string Title,
    string? Subtitle,
    string? Summary,
    string? Status,
    string RequiredPermission,
    double Score,
    DateTimeOffset UpdatedAtUtc);
