namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Response envelope returned by the Intranet BFF global search endpoint.
/// </summary>
/// <param name="Query">The normalized query used for the search.</param>
/// <param name="TotalCount">Total number of results returned by the BFF.</param>
/// <param name="Results">Search result rows that the caller can navigate to.</param>
public record GlobalSearchResponseDto(
    string Query,
    int TotalCount,
    IReadOnlyList<GlobalSearchResultDto> Results);

/// <summary>
/// Search result row displayed by the Intranet global search UI.
/// </summary>
/// <param name="Title">Primary result text.</param>
/// <param name="Subtitle">Secondary result text.</param>
/// <param name="Area">Application area that owns the result.</param>
/// <param name="ResourceType">Business resource type.</param>
/// <param name="Status">Optional business status.</param>
/// <param name="Href">Intranet route for the result.</param>
/// <param name="Score">Search relevance score.</param>
/// <param name="ThumbnailUrl">Optional display image URL for the result, such as a part thumbnail or profile image.</param>
/// <param name="AvatarText">Optional fallback avatar text when no thumbnail image is available.</param>
public record GlobalSearchResultDto(
    string Title,
    string? Subtitle,
    string Area,
    string ResourceType,
    string? Status,
    string Href,
    double Score,
    string? ThumbnailUrl = null,
    string? AvatarText = null);
