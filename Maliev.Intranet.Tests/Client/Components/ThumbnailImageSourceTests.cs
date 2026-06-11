namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Source-level regressions for project thumbnail state handling.
/// </summary>
public sealed class ThumbnailImageSourceTests
{
    [Fact]
    public void ThumbnailImage_FallbackAndMissingSignedUrlDoNotRenderIndefiniteSpinner()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "ThumbnailImage.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains("else if (_progress?.Stage == ThumbnailGenerationStage.Fallback)", source, StringComparison.Ordinal);
        Assert.Contains("Icons.Material.Outlined.BrokenImage", source, StringComparison.Ordinal);
        Assert.Contains("new ThumbnailProgress(StoragePath, ThumbnailGenerationStage.Failed, 0, \"Thumbnail unavailable\")", source, StringComparison.Ordinal);
        Assert.Contains("if (_thumbnails is not { HasAny: true })", source, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine([current.FullName, .. relativeParts]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
            current = current.Parent;
        }

        throw new FileNotFoundException(
            $"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
