using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// BFF endpoint for global Intranet search.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/search")]
public class SearchController(SearchServiceClient searchServiceClient, GlobalSearchResultEnricher searchResultEnricher) : ControllerBase
{
    /// <summary>
    /// Searches the global index and maps results to Intranet routes.
    /// </summary>
    /// <param name="query">Keyword, identifier, or name to search for.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Search results visible to the caller.</returns>
    [HttpGet]
    [RequirePermission(MalievPermissions.Search.Read)]
    public async Task<ActionResult<GlobalSearchResponseDto>> Search(
        [FromQuery] string? query,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (normalizedQuery.Length < 2)
        {
            return Ok(new GlobalSearchResponseDto(normalizedQuery, 0, []));
        }

        if (normalizedQuery.Length > 120)
        {
            return BadRequest("Search query must be 120 characters or fewer.");
        }

        var normalizedLimit = Math.Clamp(limit, 1, 50);
        var response = await searchServiceClient.SearchAsync(normalizedQuery, normalizedLimit, ct);

        return Ok(await searchResultEnricher.ToGlobalSearchResponseAsync(response, normalizedQuery, ct));
    }
}
