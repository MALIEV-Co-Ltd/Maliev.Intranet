namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Source-level regressions for the local-first thumbnail pipeline in ProjectNew.
/// Thumbnails are generated in the browser (from the original upload, or from the
/// server viewer GLB for formats the browser cannot parse). Server preview URLs
/// must never overwrite a locally generated set.
/// </summary>
public sealed class ProjectNewLocalThumbnailSourceTests
{
    private static readonly string[] RequiredProjectNew3dExtensions =
    [
        ".stl",
        ".step",
        ".stp",
        ".3mf",
        ".obj",
        ".igs",
        ".iges",
        ".fbx",
        ".glb",
        ".gltf"
    ];

    private static readonly string[] DirectBrowserThumbnailExtensions =
    [
        ".stl",
        ".obj",
        ".glb",
        ".gltf",
        ".3mf"
    ];

    private static readonly string[] ProjectNewPath =
        ["Maliev.Intranet.Client", "Pages", "ProjectNew.razor.cs"];

    [Fact]
    public void ApplyPreviewStatusKeepsLocallyGeneratedThumbnails()
    {
        var source = ReadRepoFile(ProjectNewPath);

        Assert.Contains("HasLocalThumbnails(part)", source, StringComparison.Ordinal);
        Assert.Contains("keepLocalThumbnails", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GlbReadyHandlerGeneratesThumbnailsFromViewerGlbForServerConvertedFormats()
    {
        var source = ReadRepoFile(ProjectNewPath);

        Assert.Contains("TriggerLocalThumbnailsFromViewer(part, payload.GlbUrl)", source, StringComparison.Ordinal);
        Assert.Contains("CanGenerateThumbnailsLocally", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UploadCompletionOnlyTriggersImmediateGenerationForBrowserRenderableFormats()
    {
        var source = ReadRepoFile(ProjectNewPath);

        Assert.Contains(
            "CanGenerateThumbnailsLocally(completedUpload.StoragePath)",
            source,
            StringComparison.Ordinal);
        Assert.Contains("part.SignedDownloadUrl = signedUrl", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LocallyRenderableExtensionsMatchGeometryInteropSupportedExtensions()
    {
        var projectNew = ReadRepoFile(ProjectNewPath);
        var interop = ReadRepoFile(
            "Maliev.Intranet.Client", "wwwroot", "js", "geometry", "JsInterop", "GeometryInterop.js");

        foreach (var extension in DirectBrowserThumbnailExtensions)
        {
            Assert.Contains($"\"{extension}\"", projectNew, StringComparison.Ordinal);
            Assert.Contains($"'{extension}'", interop, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EverySupportedProjectNew3dExtensionHasAThumbnailPath()
    {
        var fileTypes = ReadRepoFile("Maliev.Intranet.Tests", "Client", "Pages", "ProjectNewAutoSaveTests.cs");
        var projectNew = ReadRepoFile(ProjectNewPath);

        foreach (var extension in RequiredProjectNew3dExtensions)
        {
            Assert.Contains($"\"{extension}\"", fileTypes, StringComparison.Ordinal);
        }

        foreach (var extension in DirectBrowserThumbnailExtensions)
        {
            Assert.Contains($"\"{extension}\"", projectNew, StringComparison.Ordinal);
        }

        Assert.Contains("!CanGenerateThumbnailsLocally(part.StoragePath)", projectNew, StringComparison.Ordinal);
        Assert.Contains("CanGenerateThumbnailsLocally(viewerStoragePath ?? part.ViewerFileExtension)", projectNew, StringComparison.Ordinal);
        Assert.Contains("TriggerLocalThumbnailsFromViewer(part, payload.GlbUrl)", projectNew, StringComparison.Ordinal);
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
