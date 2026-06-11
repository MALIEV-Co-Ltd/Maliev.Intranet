namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Source-level regressions for the browser-local thumbnail generation interop script.
/// The script is loaded as a CLASSIC script tag (no ES modules), so module syntax or a
/// missing global registration silently disables local generation and forces every
/// thumbnail onto the server fallback path.
/// </summary>
public sealed class GeometryInteropSourceTests
{
    private static readonly string[] InteropPath =
        ["Maliev.Intranet.Client", "wwwroot", "js", "geometry", "JsInterop", "GeometryInterop.js"];

    private static readonly string[] IndexHtmlPath =
        ["Maliev.Intranet.Client", "wwwroot", "index.html"];

    [Fact]
    public void GeometryInteropIsAClassicScriptWithoutModuleSyntax()
    {
        var source = ReadRepoFile(InteropPath).ReplaceLineEndings("\n");

        Assert.DoesNotContain("\nimport ", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\nexport ", source, StringComparison.Ordinal);
        Assert.False(
            source.TrimStart().StartsWith("import ", StringComparison.Ordinal),
            "GeometryInterop.js must not use ES module imports — it is loaded as a classic script.");
    }

    [Fact]
    public void GeometryInteropRegistersMalievGeometryGlobal()
    {
        var source = ReadRepoFile(InteropPath);

        Assert.Contains("window.MalievGeometry", source, StringComparison.Ordinal);
        Assert.Contains("generateThumbnails", source, StringComparison.Ordinal);
        Assert.Contains("isWebGL2Available", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GeometryInteropProducesAllEightThumbnailSetViews()
    {
        var source = ReadRepoFile(InteropPath);

        // Keys must match Maliev.Intranet.Shared.Dtos.ThumbnailSetDto (camelCase).
        string[] requiredKeys =
        [
            "frontSmall", "backSmall", "leftSmall", "rightSmall",
            "topSmall", "bottomSmall", "thumbnailSmall", "thumbnailLarge",
        ];
        foreach (var key in requiredKeys)
        {
            Assert.Contains(key, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void GeometryInteropCapturesThumbnailsWithTransparentAlphaBackground()
    {
        var source = ReadRepoFile(InteropPath);

        Assert.Contains("preserveDrawingBuffer: true", source, StringComparison.Ordinal);
        Assert.Contains("premultipliedAlpha: false", source, StringComparison.Ordinal);
        Assert.Contains("alpha: true", source, StringComparison.Ordinal);
        Assert.Contains("scene.clearColor = new BABYLON.Color4(0, 0, 0, 0)", source, StringComparison.Ordinal);
        Assert.Contains("toDataURL('image/png')", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GeometryInteropUsesZUpViewConvention()
    {
        var source = ReadRepoFile(InteropPath);

        // MALIEV models are Z-up: face views must use a Z-up camera up-vector.
        Assert.Contains("up: [0, 0, 1]", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("index.html host page")]
    [InlineData("BFF App.razor host page")]
    public void HostPagesLoadBabylonBeforeGeometryInterop(string hostPage)
    {
        // InteractiveAuto serves the BFF's App.razor, NOT the WASM index.html.
        // GeometryInterop.js missing from the served page leaves window.MalievGeometry
        // undefined and silently disables ALL local thumbnail generation.
        var html = hostPage.StartsWith("index", StringComparison.Ordinal)
            ? ReadRepoFile(IndexHtmlPath)
            : ReadRepoFile("Maliev.Intranet.Bff", "Components", "App.razor");

        var babylonIndex = html.IndexOf("lib/babylonjs/babylon.js", StringComparison.Ordinal);
        var loadersIndex = html.IndexOf("lib/babylonjs/babylonjs.loaders.min.js", StringComparison.Ordinal);
        var interopIndex = html.IndexOf("js/geometry/JsInterop/GeometryInterop.js", StringComparison.Ordinal);

        Assert.True(babylonIndex >= 0, $"{hostPage} must load the BabylonJS runtime.");
        Assert.True(loadersIndex >= 0, $"{hostPage} must load the BabylonJS mesh loaders.");
        Assert.True(interopIndex >= 0, $"{hostPage} must load GeometryInterop.js.");
        Assert.True(
            babylonIndex < interopIndex && loadersIndex < interopIndex,
            $"GeometryInterop.js depends on the global BABYLON runtime and must load after babylon.js and the loaders in {hostPage}.");
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
