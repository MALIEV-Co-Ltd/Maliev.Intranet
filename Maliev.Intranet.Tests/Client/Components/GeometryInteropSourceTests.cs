namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Source-level regressions for the browser-local thumbnail generation interop script.
/// The script is loaded as an ES module (type="module") so it can import three.js via
/// the importmap. A missing global registration silently disables local generation and
/// forces every thumbnail onto the server fallback path.
/// </summary>
public sealed class GeometryInteropSourceTests
{
    private static readonly string[] InteropPath =
        ["Maliev.Intranet.Client", "wwwroot", "js", "geometry", "JsInterop", "GeometryInterop-three.js"];

    private static readonly string[] IndexHtmlPath =
        ["Maliev.Intranet.Client", "wwwroot", "index.html"];

    [Fact]
    public void GeometryInteropIsAnEsModuleWithThreeImport()
    {
        // The script migrated from BabylonJS (classic script) to three.js (ES module)
        // so it can consume the importmap-based three.js build without a global BABYLON runtime.
        var source = ReadRepoFile(InteropPath).ReplaceLineEndings("\n");

        Assert.Contains("import * as THREE from 'three'", source, StringComparison.Ordinal);
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

        // three.js WebGLRenderer: alpha:true → transparent canvas; preserveDrawingBuffer
        // keeps the pixel data available for toDataURL after each frame.
        Assert.Contains("preserveDrawingBuffer: true", source, StringComparison.Ordinal);
        Assert.Contains("alpha: true", source, StringComparison.Ordinal);
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
    public void HostPagesLoadThreeJsImportmapAndGeometryInteropModule(string hostPage)
    {
        // InteractiveAuto serves the BFF's App.razor, NOT the WASM index.html.
        // GeometryInterop-three.js is loaded as type="module" and depends on the
        // importmap mapping "three" to the vendored three.module.min.js.
        var html = hostPage.StartsWith("index", StringComparison.Ordinal)
            ? ReadRepoFile(IndexHtmlPath)
            : ReadRepoFile("Maliev.Intranet.Bff", "Components", "App.razor");

        var importMapIndex = html.IndexOf("\"three\"", StringComparison.Ordinal);
        var interopIndex = html.IndexOf("js/geometry/JsInterop/GeometryInterop-three.js", StringComparison.Ordinal);

        Assert.True(importMapIndex >= 0, $"{hostPage} must include the three.js importmap so GeometryInterop-three.js can resolve its import.");
        Assert.True(interopIndex >= 0, $"{hostPage} must load GeometryInterop-three.js as a module script.");
        Assert.Contains("type=\"importmap\"", html, StringComparison.Ordinal);
        Assert.Contains("\"three\": \"/lib/three/build/three.module.min.js\"", html, StringComparison.Ordinal);
        Assert.Contains("\"three/addons/\": \"/lib/three/jsm/\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("\"three\": \"lib/three", html, StringComparison.Ordinal);
        Assert.DoesNotContain("\"three/addons/\": \"lib/three", html, StringComparison.Ordinal);
        Assert.Contains("type=\"module\"", html, StringComparison.Ordinal);
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
