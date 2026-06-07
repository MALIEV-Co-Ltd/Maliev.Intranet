namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Progress event for thumbnail generation, used to update UI components.
/// </summary>
/// <param name="StoragePath">GCS storage path of the source file.</param>
/// <param name="Stage">Current generation stage.</param>
/// <param name="Percent">Progress percentage (0-100).</param>
/// <param name="Message">Human-readable status message.</param>
public sealed record ThumbnailProgress(
    string StoragePath,
    ThumbnailGenerationStage Stage,
    int Percent,
    string? Message);