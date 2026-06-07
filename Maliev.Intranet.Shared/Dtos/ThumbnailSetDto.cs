using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Set of 8 thumbnail views generated client-side or server-side.
/// All fields are base64-encoded JPEG DataURLs (data:image/jpeg;base64,...).
/// </summary>
public sealed class ThumbnailSetDto
{
    /// <summary>Front-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string FrontSmall { get; set; } = string.Empty;

    /// <summary>Back-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string BackSmall { get; set; } = string.Empty;

    /// <summary>Left-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string LeftSmall { get; set; } = string.Empty;

    /// <summary>Right-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string RightSmall { get; set; } = string.Empty;

    /// <summary>Top-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string TopSmall { get; set; } = string.Empty;

    /// <summary>Bottom-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string BottomSmall { get; set; } = string.Empty;

    /// <summary>Isometric thumbnail small (256x256).</summary>
    [MaxLength(2_000_000)]
    public string ThumbnailSmall { get; set; } = string.Empty;

    /// <summary>Isometric thumbnail large (1200x1200).</summary>
    [MaxLength(5_000_000)]
    public string ThumbnailLarge { get; set; } = string.Empty;

    /// <summary>Content version hash for cache invalidation.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>True if any thumbnail is non-empty (used by UI to determine "ready" state).</summary>
    public bool HasAny => !string.IsNullOrEmpty(ThumbnailSmall) || !string.IsNullOrEmpty(FrontSmall);
}