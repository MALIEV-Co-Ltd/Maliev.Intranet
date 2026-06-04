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
        Assert.Contains("const positionTolerance = getSmoothNormalPositionTolerance(positions, options);", source, StringComparison.Ordinal);
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
    public void RealisticEnvironment_UsesLightweightProceduralStudioCube()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("environmentTextureSize: 256", source, StringComparison.Ordinal);
        Assert.Contains("fastEnvironmentTextureSize", source, StringComparison.Ordinal);
        Assert.Contains("const highSize = CONFIG.REALISTIC.environmentTextureSize || 256;", source, StringComparison.Ordinal);
        Assert.Contains("const size = quality === 'high' ? highSize : fastSize;", source, StringComparison.Ordinal);
        Assert.Contains("function studioColor(direction)", source, StringComparison.Ordinal);
        Assert.Contains("cube._malievEnvironmentKind = 'procedural-studio';", source, StringComparison.Ordinal);
        Assert.DoesNotContain("environmentTextureSize: 512", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticPbrMaterials_EnableSpecularAntiAliasingWithoutExpensiveRealtimeFiltering()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("function configureRealisticPbrQuality(pbr)", source, StringComparison.Ordinal);
        Assert.Contains("pbr.enableSpecularAntiAliasing = true;", source, StringComparison.Ordinal);
        Assert.Contains("configureRealisticPbrQuality(pbr);", source, StringComparison.Ordinal);

        // realTimeFiltering was removed: it ran 64-sample per-pixel IBL prefiltering on a
        // static, already-prefiltered studio cube — expensive on mobile and a driver-dependent
        // instability source (the realistic-mode flicker). Lock the regression out.
        Assert.DoesNotContain("pbr.realTimeFiltering", source, StringComparison.Ordinal);
        Assert.DoesNotContain("realTimeFilteringQuality", source, StringComparison.Ordinal);
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
        Assert.Contains("ResolveBrowserFileViewerExtension(part)", projectNew, StringComparison.Ordinal);
        Assert.Contains("NormalizeViewerFileExtension(null, part.StoragePath ?? part.Name)", projectNew, StringComparison.Ordinal);
        Assert.Contains("PersistableViewerUrl(ViewerUrl)", partViewModel, StringComparison.Ordinal);
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

    [Fact]
    public void BrowserLocalDfmCallback_UsesBlazorAcceptanceBeforeClearingPanel()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("const accepted = await dotNetRef.invokeMethodAsync('NotifyLocalGeometryRuntimeComplete', result);", source, StringComparison.Ordinal);
        Assert.Contains("return accepted === true;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserLocalDfmTelemetry_PostsCompletionToSameOriginBff()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("const LOCAL_ADVISORY_TELEMETRY_URL = '/api/v1/geometry/runtime/telemetry';", source, StringComparison.Ordinal);
        Assert.Contains("function postLocalAdvisoryTelemetry(endpointUrl, payload)", source, StringComparison.Ordinal);
        Assert.Contains("navigator.sendBeacon", source, StringComparison.Ordinal);
        Assert.Contains("accepted,", source, StringComparison.Ordinal);
        Assert.Contains("storagePath: result?.storagePath ?? null", source, StringComparison.Ordinal);
        Assert.Contains("metrics: result?.metrics ?? null", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserLocalDfmTelemetry_PostsStartToSameOriginBff()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("function dispatchLocalAdvisoryStartedTelemetry(payload)", source, StringComparison.Ordinal);
        Assert.Contains("status: 'started'", source, StringComparison.Ordinal);
        Assert.Contains("inputByteCount: payload?.inputByteCount ?? null", source, StringComparison.Ordinal);
        Assert.Contains("inputTriangleCount: payload?.inputTriangleCount ?? null", source, StringComparison.Ordinal);
        Assert.Contains("dispatchLocalAdvisoryStartedTelemetry(payload);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserLocalDfmTelemetry_PostsTerminalUnavailableToSameOriginBff()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("function dispatchLocalAdvisoryUnavailableTelemetry(payload)", source, StringComparison.Ordinal);
        Assert.Contains("status: 'unavailable'", source, StringComparison.Ordinal);
        Assert.Contains("reason: payload?.reason ?? 'local_runtime_unavailable'", source, StringComparison.Ordinal);
        Assert.Contains("dispatchLocalAdvisoryUnavailableTelemetry(payload);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserLocalDfmRuntime_UsesManifestDeviceProfileTimeout()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("function resolveLocalAdvisoryDeviceProfileName()", source, StringComparison.Ordinal);
        Assert.Contains("function resolveLocalAdvisoryTimeoutMs(manifest, options = {})", source, StringComparison.Ordinal);
        Assert.Contains("manifest?.deviceProfiles", source, StringComparison.Ordinal);
        Assert.Contains("resolveLocalAdvisoryTimeoutMs(manifest, options));", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Number(options.timeoutMs) > 0 ? Number(options.timeoutMs) : 15000",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserLocalDfmRuntime_HonorsManifestDeviceInputLimits()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("function isLocalAdvisoryInputWithinDeviceProfile(manifest, input)", source, StringComparison.Ordinal);
        Assert.Contains("profile?.maxInputBytes", source, StringComparison.Ordinal);
        Assert.Contains("profile?.maxTriangles", source, StringComparison.Ordinal);
        Assert.Contains("countLocalAdvisoryInputTriangles(input)", source, StringComparison.Ordinal);
        Assert.Contains("if (!isLocalAdvisoryInputWithinDeviceProfile(manifest, runtimeInput))", source, StringComparison.Ordinal);
        Assert.Contains("clearLocalAdvisoryPanel(canvasId);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserLocalDfmRuntime_NotifiesBlazorWhenLocalAttemptCannotRun()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("function notifyLocalAdvisoryUnavailableDotNet(dotNetRef, payload)", source, StringComparison.Ordinal);
        Assert.Contains("NotifyLocalGeometryRuntimeUnavailable", source, StringComparison.Ordinal);
        Assert.Contains("options.dotNetRef", source, StringComparison.Ordinal);
        Assert.Contains("'input_too_large'", source, StringComparison.Ordinal);
        Assert.Contains("'worker_failed'", source, StringComparison.Ordinal);
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
