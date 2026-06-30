namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Source-level regressions for ModelViewer.razor's configurator/material-forwarding
/// and browser-local-DFM-runtime contracts. The BabylonJS-specific rendering-internals
/// tests that used to live here (Realistic mode, shadow/lighting, grid floor, turning-axis
/// SVG overlay, staged-render transitions) were removed along with wwwroot/js/part-viewer.js
/// when the viewer migrated to three.js (see wwwroot/js/part-viewer-three.js) — "Realistic"
/// mode no longer exists anywhere, and the remaining render internals have a different
/// structure under three.js that isn't meaningfully tested at this string-matching level.
/// </summary>
public sealed class ModelViewerRenderingSourceTests
{
    private static string ModelViewer => ReadRepoFile("Maliev.Intranet.Client", "Components", "ModelViewer.razor");

    [Fact]
    public void ProjectNewModelViewer_DoesNotDefaultToReducedMotion()
    {
        var modelViewer = ModelViewer.ReplaceLineEndings("\n");

        Assert.DoesNotContain("prefers-reduced-motion", modelViewer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reducedMotion", modelViewer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SolidMode_IsTheInitialModelViewerRenderModeAndNormalizerFallback()
    {
        // "Realistic" PBR rendering was removed (too heavy, no usefulness per product
        // decision) — solid/wireframe/transparent are the only render modes now, and
        // "solid" is both the initial mode and the fallback for any unrecognized or
        // legacy-persisted value (including previously-persisted "realistic" settings).
        var source = ModelViewer.ReplaceLineEndings("\n");
        var csharpNormalizer = ExtractRenderModeNormalizer(source);

        Assert.Contains("private string _renderMode        = \"solid\";", source, StringComparison.Ordinal);
        Assert.Contains("\"wireframe\"", csharpNormalizer, StringComparison.Ordinal);
        Assert.Contains("\"transparent\"", csharpNormalizer, StringComparison.Ordinal);
        Assert.Contains(": \"solid\";", csharpNormalizer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"realistic\"", csharpNormalizer, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserDfmFallbackRuntime_WeldsOnlyWithinSourceMeshBuffers()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "GeometryRuntimeFallback", "client-geometry-runtime.worker.js")
            .ReplaceLineEndings("\n");

        Assert.Contains("const sourceGroups = [];", source, StringComparison.Ordinal);
        Assert.Contains("sourceGroups.push({", source, StringComparison.Ordinal);
        Assert.Contains("function buildWeldedIndexMap(positions, sourceGroups = null)", source, StringComparison.Ordinal);
        Assert.Contains("function buildSourceGroupLookup(vertexCount, sourceGroups)", source, StringComparison.Ordinal);
        Assert.Contains("const key = `${groupByVertex[vertex]}:` +", source, StringComparison.Ordinal);
        Assert.Contains("const weldedIndex = buildWeldedIndexMap(positions, mesh.sourceGroups);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PartViewerSettings_DtoIncludesRenderModeTransitionFields()
    {
        var dtoSource = ReadRepoFile("Maliev.Intranet.Shared", "Dtos", "ProjectDraftDtos.cs");

        Assert.Contains("public string InitialRenderMode", dtoSource, StringComparison.Ordinal);
        Assert.Contains("public string TargetRenderMode", dtoSource, StringComparison.Ordinal);
        Assert.Contains("public RenderModeTransitionSettings RenderModeTransition", dtoSource, StringComparison.Ordinal);
        Assert.Contains("public bool Enabled", dtoSource, StringComparison.Ordinal);
        Assert.Contains("public string Trigger", dtoSource, StringComparison.Ordinal);
        Assert.Contains("public int FallbackDelayMs", dtoSource, StringComparison.Ordinal);
        Assert.Contains("public int TransitionMs", dtoSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ModelViewer_CaptureViewerRuntimeSettings_IncludesNewFields()
    {
        var source = ModelViewer.ReplaceLineEndings("\n");

        Assert.Contains("settings.InitialRenderMode,", source, StringComparison.Ordinal);
        Assert.Contains("settings.TargetRenderMode,", source, StringComparison.Ordinal);
        Assert.Contains("settings.RenderModeTransition,", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ModelViewer_GetViewerSettingsKey_IncludesNewFields()
    {
        var source = ModelViewer.ReplaceLineEndings("\n");

        Assert.Contains("NormalizeRenderMode(settings.InitialRenderMode)", source, StringComparison.Ordinal);
        Assert.Contains("NormalizeRenderMode(settings.TargetRenderMode)", source, StringComparison.Ordinal);
        Assert.Contains("settings.RenderModeTransition?.Enabled", source, StringComparison.Ordinal);
        Assert.Contains("settings.RenderModeTransition?.FallbackDelayMs", source, StringComparison.Ordinal);
        Assert.Contains("settings.RenderModeTransition?.TransitionMs", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticConfigurator_ForwardsProcessCodeForSurfaceEffects()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "ModelViewer.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains(
            "_canvasId, _materialType, colorHex, finishCode ?? string.Empty, roughnessCode, processCode",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticConfigurator_MapsPowderBedProcessesToNylonPowderPreset()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "ModelViewer.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains("\"nylon-powder\" => \"nylon-powder\"", source, StringComparison.Ordinal);
        Assert.Contains("key is \"MJF\" or \"SLS\" or \"SLS_PA\" or \"SJS\" => \"nylon-powder\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticConfigurator_MapsTransparentMaterialsToTransparentPresets()
    {
        var source = ModelViewer.ReplaceLineEndings("\n");
        var mapping = ExtractExpression(
            source,
            "private static string MapConfiguratorMaterialToPreset(string? processCode, string? materialCode, string? colorHex)",
            "    private async Task ResetCamera()");

        Assert.Contains("\"petg-clear\" => \"petg-clear\"", source, StringComparison.Ordinal);
        Assert.Contains("\"acrylic-clear\" => \"acrylic-clear\"", source, StringComparison.Ordinal);
        Assert.Contains("\"resin-clear\" => \"resin-clear\"", source, StringComparison.Ordinal);
        Assert.Contains("private static bool IsClearMaterialSelection(string materialCode, string colorHex)", source, StringComparison.Ordinal);
        Assert.Contains("_ when IsCncAcrylicMaterial(mat) => \"acrylic-clear\"", mapping, StringComparison.Ordinal);
        Assert.Contains("_ when (key is \"SLA\" or \"SLA_DLP\" or \"DLP\") && IsClearMaterialSelection(mat, colorLower) => \"resin-clear\"", mapping, StringComparison.Ordinal);
        Assert.Contains("_ when mat.Contains(\"PETG\") && IsClearMaterialSelection(mat, colorLower) => \"petg-clear\"", mapping, StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticConfigurator_MapsPeekBeforeProcessSpecificFallbacks()
    {
        var source = ModelViewer.ReplaceLineEndings("\n");
        var mapping = ExtractExpression(
            source,
            "private static string MapConfiguratorMaterialToPreset(string? processCode, string? materialCode, string? colorHex)",
            "    private async Task ResetCamera()");

        var peekIndex = mapping.IndexOf("_ when mat.Contains(\"PEEK\") => \"peek\"", StringComparison.Ordinal);
        var fdmFallbackIndex = mapping.IndexOf("_ when key is \"FDM\" or \"FDM_3D_PRINTING\" =>", StringComparison.Ordinal);

        Assert.True(peekIndex >= 0, "PEEK must map to the intrinsic tan PEEK material preset.");
        Assert.True(fdmFallbackIndex >= 0, "Unable to locate the FDM fallback branch.");
        Assert.True(
            peekIndex < fdmFallbackIndex,
            "PEEK must be mapped before process-specific fallbacks so CNC PEEK does not render as aluminum.");
    }

    [Fact]
    public void RealisticConfigurator_PartDetailCardRepushesWhenProcessChanges()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartDetailCard.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains("materialChanged || colorChanged || finishChanged || roughnessChanged || processChanged || initialPush", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ModelViewer_ForwardsBrowserUploadFileReferenceToLocalRuntime()
    {
        var modelViewer = ModelViewer.ReplaceLineEndings("\n");
        var partDetail = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartDetailCard.razor")
            .ReplaceLineEndings("\n");
        var uploadHelper = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "js", "uploadWithProgress.js")
            .ReplaceLineEndings("\n");
        var projectNew = ReadRepoFile("Maliev.Intranet.Client", "Pages", "ProjectNew.razor.cs")
            .ReplaceLineEndings("\n");
        var partViewModel = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartViewModel.cs")
            .ReplaceLineEndings("\n");

        Assert.Contains("[Parameter] public string?                    BrowserFileClientId", modelViewer, StringComparison.Ordinal);
        Assert.Contains("[Parameter] public string?                    BrowserFileName", modelViewer, StringComparison.Ordinal);
        Assert.Contains("[Parameter] public string?                    StoragePath", modelViewer, StringComparison.Ordinal);
        Assert.Contains("clientUploadId = BrowserFileClientId", modelViewer, StringComparison.Ordinal);
        Assert.Contains("fileName = BrowserFileName", modelViewer, StringComparison.Ordinal);
        Assert.Contains("storagePath = StoragePath", modelViewer, StringComparison.Ordinal);
        Assert.Contains("fileBytesProvider = \"projectNewUploads\"", modelViewer, StringComparison.Ordinal);
        Assert.Contains("BrowserFileClientId=\"@Part.ClientUploadId\"", partDetail, StringComparison.Ordinal);
        Assert.Contains("BrowserFileName=\"@Part.Name\"", partDetail, StringComparison.Ordinal);
        Assert.Contains("StoragePath=\"@Part.StoragePath\"", partDetail, StringComparison.Ordinal);
        Assert.Contains("async function getFileBytes(clientUploadId)", uploadHelper, StringComparison.Ordinal);
        Assert.Contains("function getObjectUrl(clientUploadId)", uploadHelper, StringComparison.Ordinal);
        Assert.Contains("scheduleClearFile", uploadHelper, StringComparison.Ordinal);
        Assert.Contains("TryApplyLocalViewerUrlAsync(part)", projectNew, StringComparison.Ordinal);
        Assert.Contains("window.projectNewUploads.getObjectUrl", projectNew, StringComparison.Ordinal);
        Assert.Contains("CanUseBrowserFileViewer", projectNew, StringComparison.Ordinal);
        Assert.Contains("ResolveBrowserFileViewerExtensionAsync(part)", projectNew, StringComparison.Ordinal);
        Assert.Contains("api/v1/geometry/runtime/manifest", projectNew, StringComparison.Ordinal);
        Assert.Contains("artifactPolicy", projectNew, StringComparison.Ordinal);
        Assert.Contains("directBrowserViewerExtensions", projectNew, StringComparison.Ordinal);
        Assert.Contains("DefaultBrowserViewerExtensions", projectNew, StringComparison.Ordinal);
        Assert.Contains("PersistableViewerUrl(ViewerUrl)", partViewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void ModelViewer_InitializationPayloadIncludesBrowserLocalRuntimeMetadata()
    {
        var modelViewer = ModelViewer.ReplaceLineEndings("\n");
        var captureBlock = ExtractBlock(modelViewer, "private object CaptureViewerRuntimeSettings");

        Assert.Contains("ProcessCode", captureBlock, StringComparison.Ordinal);
        Assert.Contains("storagePath = StoragePath", captureBlock, StringComparison.Ordinal);
        Assert.Contains("browserFileClientId = BrowserFileClientId", captureBlock, StringComparison.Ordinal);
        Assert.Contains("browserFileName = BrowserFileName", captureBlock, StringComparison.Ordinal);
        Assert.Contains("fileBytesProvider = \"projectNewUploads\"", captureBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectNew_RetainsBrowserViewerFilesUntilPartLifecycleEnds()
    {
        var projectNew = ReadRepoFile("Maliev.Intranet.Client", "Pages", "ProjectNew.razor.cs")
            .ReplaceLineEndings("\n");

        Assert.Contains("ShouldRetainBrowserUploadFile(part)", projectNew, StringComparison.Ordinal);
        Assert.Contains("!ShouldRetainBrowserUploadFile(part)", projectNew, StringComparison.Ordinal);
        Assert.Contains("ClearBrowserUploadFileAsync(part)", projectNew, StringComparison.Ordinal);
        Assert.Contains("window.projectNewUploads.clearFile", projectNew, StringComparison.Ordinal);
        Assert.Contains("ClientUploadId = sourcePart.ClientUploadId", projectNew, StringComparison.Ordinal);
        Assert.Contains("!ReferenceEquals(existing, part)", projectNew, StringComparison.Ordinal);
    }

    private static string ExtractBlock(string source, string start)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"Unable to locate block start: {start}");

        var depth = 0;
        for (var i = startIndex; i < source.Length; i++)
        {
            if (source[i] == '{')
                depth++;
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return source[startIndex..(i + 1)];
            }
        }

        throw new InvalidDataException($"Unable to extract block starting at: {start}");
    }

    private static string ExtractRenderModeNormalizer(string source)
        => ExtractExpression(source, "private static string NormalizeRenderMode(string? mode) =>", "private static string NormalizeProjection");

    private static string ExtractExpression(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"Unable to locate expression start: {start}");

        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        Assert.True(endIndex > startIndex, $"Unable to locate expression end: {end}");

        return source[startIndex..endIndex];
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        return File.ReadAllText(FindRepoFile(relativeParts));
    }

    private static string FindRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return candidate;
            current = current.Parent;
        }

        throw new FileNotFoundException(
            $"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
