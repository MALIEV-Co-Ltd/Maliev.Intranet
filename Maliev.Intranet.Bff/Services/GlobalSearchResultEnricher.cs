using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Adds Intranet presentation data that should not be stored in the global search index.
/// </summary>
/// <param name="projectServiceClient">Client used to resolve current project part details.</param>
/// <param name="uploadServiceClient">Client used to resolve signed thumbnail URLs from stored artifact paths.</param>
/// <param name="logger">Logger for non-fatal enrichment failures.</param>
public class GlobalSearchResultEnricher(
    ProjectServiceClient projectServiceClient,
    UploadServiceClient uploadServiceClient,
    ILogger<GlobalSearchResultEnricher> logger)
{
    /// <summary>
    /// Maps SearchService rows to Intranet rows and enriches project-part thumbnails.
    /// </summary>
    /// <param name="response">Downstream SearchService response.</param>
    /// <param name="query">Normalized query text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Enriched Intranet global search response.</returns>
    public async Task<GlobalSearchResponseDto> ToGlobalSearchResponseAsync(
        SearchServiceResponseDto? response,
        string query,
        CancellationToken ct)
    {
        if (response is null)
        {
            return new GlobalSearchResponseDto(query, 0, []);
        }

        var resultPairs = response.Results
            .Select(result => new SearchResultPair(result, SearchResultMapper.ToGlobalSearchResult(result)))
            .ToList();

        await EnrichProjectPartThumbnailsAsync(resultPairs, ct);

        var results = resultPairs
            .Select(pair => pair.Result)
            .ToList();

        return new GlobalSearchResponseDto(response.Query, results.Count, results);
    }

    private async Task EnrichProjectPartThumbnailsAsync(List<SearchResultPair> resultPairs, CancellationToken ct)
    {
        var partRefs = resultPairs
            .Select((pair, index) => new { pair.Source, Index = index })
            .Where(item => IsProjectPart(item.Source.ResourceType) &&
                SearchResultMapper.TryResolveProjectPartId(item.Source.ResourceId, out _, out _))
            .Select(item =>
            {
                SearchResultMapper.TryResolveProjectPartId(item.Source.ResourceId, out var projectId, out var partId);
                return new ProjectPartReference(item.Index, projectId, partId);
            })
            .ToList();

        if (partRefs.Count == 0)
        {
            return;
        }

        var projects = new Dictionary<Guid, ProjectDetailDto?>();
        foreach (var projectId in partRefs.Select(item => item.ProjectId).Distinct())
        {
            try
            {
                projects[projectId] = await projectServiceClient.GetProjectByIdAsync(projectId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to enrich search thumbnails for project {ProjectId}", projectId);
                projects[projectId] = null;
            }
        }

        foreach (var partRef in partRefs)
        {
            if (!projects.TryGetValue(partRef.ProjectId, out var project) || project is null)
            {
                continue;
            }

            var part = project.Parts.FirstOrDefault(item => item.Id == partRef.PartId);
            if (part is null)
            {
                continue;
            }

            var thumbnailUrl = await ResolveThumbnailUrlAsync(part, ct);
            if (string.IsNullOrWhiteSpace(thumbnailUrl))
            {
                continue;
            }

            var current = resultPairs[partRef.ResultIndex].Result;
            resultPairs[partRef.ResultIndex] = resultPairs[partRef.ResultIndex] with
            {
                Result = current with { ThumbnailUrl = thumbnailUrl }
            };
        }
    }

    private async Task<string?> ResolveThumbnailUrlAsync(ProjectPartDto part, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(part.ThumbnailUrl))
        {
            return part.ThumbnailUrl;
        }

        var path = FirstNonEmpty(part.ThumbnailSmallGcsPath, part.ThumbnailLargeGcsPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return await uploadServiceClient.GetDownloadUrlByPathAsync(path, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to resolve search thumbnail URL for path {StoragePath}", path);
            return null;
        }
    }

    private static bool IsProjectPart(string resourceType)
    {
        var normalized = resourceType.Trim()
            .Replace("_", "-", StringComparison.Ordinal)
            .ToLowerInvariant();

        return normalized is "project-part" or "project-parts";
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private sealed record SearchResultPair(SearchServiceResultDto Source, GlobalSearchResultDto Result);

    private sealed record ProjectPartReference(int ResultIndex, Guid ProjectId, Guid PartId);
}
