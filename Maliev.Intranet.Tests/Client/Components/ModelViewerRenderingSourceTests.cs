namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Source-level regressions for realistic 3D viewer rendering quality.
/// </summary>
public sealed class ModelViewerRenderingSourceTests
{
    private static string ViewerScript => ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "js", "part-viewer.js");
    private static string ModelViewer => ReadRepoFile("Maliev.Intranet.Client", "Components", "ModelViewer.razor");

    [Fact]
    public void RealisticNormals_SmoothDuplicateCadVerticesByPosition()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("normalPositionTolerance", source, StringComparison.Ordinal);
        Assert.Contains("function getSmoothNormalPositionKey", source, StringComparison.Ordinal);
        Assert.Contains("positionFaceMap", source, StringComparison.Ordinal);
        Assert.Contains("const refNormal = normalizeNormalVector(origNorms, v * 3, faces[0]);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("const ref = faces[0]; // use first face normal as reference", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticAluminum_UsesSatinRoughnessForSmootherReflections()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");
        var aluminumBlock = ExtractBlock(source, "        'aluminum': {");

        Assert.Contains("roughness: 0.34", aluminumBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticNormals_UseAdaptiveToleranceAndAreaWeightedAveraging()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("normalPositionToleranceMin", source, StringComparison.Ordinal);
        Assert.Contains("normalPositionToleranceRatio", source, StringComparison.Ordinal);
        Assert.Contains("function getSmoothNormalPositionTolerance", source, StringComparison.Ordinal);
        Assert.Contains("const positionTolerance = getSmoothNormalPositionTolerance(positions);", source, StringComparison.Ordinal);
        Assert.Contains("weight: area", source, StringComparison.Ordinal);
        Assert.Contains("sx += f.x * weight", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticMode_IsTheInitialModelViewerRenderMode()
    {
        var source = ModelViewer.ReplaceLineEndings("\n");

        Assert.Contains("private string _renderMode        = \"realistic\";", source, StringComparison.Ordinal);
        Assert.Contains(": \"realistic\";", ExtractRenderModeNormalizer(source), StringComparison.Ordinal);
    }

    [Fact]
    public void SolidMode_RoundTripsThroughViewerSettingsNormalizer()
    {
        var csharpNormalizer = ExtractRenderModeNormalizer(ModelViewer.ReplaceLineEndings("\n"));
        var javascriptNormalizer = ExtractJavascriptRenderModeNormalizer(ViewerScript.ReplaceLineEndings("\n"));

        Assert.Contains("string.Equals(mode, \"solid\", StringComparison.OrdinalIgnoreCase)", csharpNormalizer, StringComparison.Ordinal);
        Assert.Contains("? \"solid\"", csharpNormalizer, StringComparison.Ordinal);
        Assert.Contains("settings.renderMode === 'solid'", javascriptNormalizer, StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticMode_IsTheJavascriptFallbackRenderMode()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains(": 'realistic';", ExtractJavascriptRenderModeNormalizer(source), StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticEnvironment_UsesHigherResolutionFilteredStudioCube()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("environmentTextureSize: 512", source, StringComparison.Ordinal);
        Assert.Contains("const size = CONFIG.REALISTIC.environmentTextureSize || 512;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("const size = 256;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticPbrMaterials_EnableSpecularFilteringForSmoothMetalReflections()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("function configureRealisticPbrQuality(pbr)", source, StringComparison.Ordinal);
        Assert.Contains("pbr.enableSpecularAntiAliasing = true;", source, StringComparison.Ordinal);
        Assert.Contains("pbr.realTimeFiltering = true;", source, StringComparison.Ordinal);
        Assert.Contains("pbr.realTimeFilteringQuality = BABYLON.Constants.TEXTURE_FILTERING_QUALITY_HIGH;", source, StringComparison.Ordinal);
        Assert.Contains("configureRealisticPbrQuality(pbr);", source, StringComparison.Ordinal);
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

    private static string ExtractJavascriptRenderModeNormalizer(string source)
        => ExtractExpression(source, "const renderMode =", "const cameraMode = settings.cameraProjection");

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
