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
        Assert.Contains("normalizeMode(settings.renderMode)", javascriptNormalizer, StringComparison.Ordinal);
    }

    [Fact]
    public void RealisticMode_IsTheJavascriptFallbackRenderMode()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        Assert.Contains("normalizeMode(settings.renderMode)", ExtractJavascriptRenderModeNormalizer(source), StringComparison.Ordinal);
        Assert.Contains("normalizeMode(settings.initialRenderMode)", ExtractJavascriptRenderModeNormalizer(source), StringComparison.Ordinal);
        Assert.Contains("normalizeMode(settings.targetRenderMode)", ExtractJavascriptRenderModeNormalizer(source), StringComparison.Ordinal);
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
    public void StagedRender_NormalizeViewerSettings_IncludesInitialRenderModeAndTransition()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        // Verify normalizeViewerSettings parses initialRenderMode, targetRenderMode, and renderModeTransition
        Assert.Contains("const initialRenderMode = normalizeMode(settings.initialRenderMode);", source, StringComparison.Ordinal);
        Assert.Contains("const targetRenderMode = normalizeMode(settings.targetRenderMode);", source, StringComparison.Ordinal);
        Assert.Contains("const transition = settings.renderModeTransition", source, StringComparison.Ordinal);
        Assert.Contains("transitionEnabled", source, StringComparison.Ordinal);
        Assert.Contains("fallbackDelayMs", source, StringComparison.Ordinal);
        Assert.Contains("transitionMs", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StagedRender_InitialRenderModeDefaultsToSolidWhenTargetIsRealistic()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        // Verify that when target is realistic and initial is not realistic, solid is used for first paint
        Assert.Contains("const effectiveInitialMode = (targetMode === 'realistic' && initialMode !== 'realistic')", source, StringComparison.Ordinal);
        Assert.Contains("setRenderMode(canvasId, effectiveInitialMode);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StagedRender_ScheduleFallbackTransition_UsesConfiguredDelayAndDuration()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        // Verify fallback scheduling with configurable delay and transition duration
        Assert.Contains("function scheduleRenderModeFallback(canvasId, delayMs = 1200, transitionMs = 250)", source, StringComparison.Ordinal);
        Assert.Contains("clearTimeout(state.fallbackTimer);", source, StringComparison.Ordinal);
        Assert.Contains("state.fallbackTimer = setTimeout(() =>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StagedRender_TransitionToRealistic_AnimatesAlphaFade()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        // Verify transition animates material alpha from 0 to 1
        Assert.Contains("function transitionToRealistic(canvasId, transitionMs = 250)", source, StringComparison.Ordinal);
        Assert.Contains("sharedRealisticMaterial.alpha = 0;", source, StringComparison.Ordinal);
        Assert.Contains("animateMaterialAlpha(sharedRealisticMaterial, 0, 1, scene, transitionMs);", source, StringComparison.Ordinal);
        Assert.Contains("currentRenderModes[canvasId] = 'realistic';", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StagedRender_RuntimeComplete_CancelsFallbackAndTransitions()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        // Verify that on runtime complete, fallback timer is cleared and transition occurs
        Assert.Contains("if (transitionState.fallbackTimer) {", source, StringComparison.Ordinal);
        Assert.Contains("clearTimeout(transitionState.fallbackTimer);", source, StringComparison.Ordinal);
        Assert.Contains("transitionState.fallbackTimer = null;", source, StringComparison.Ordinal);
        Assert.Contains("transitionToRealistic(canvasId, transitionState.transitionMs);", source, StringComparison.Ordinal);
        Assert.Contains("transitionState.completed = true;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StagedRender_ManualModeChange_UpdatesTransitionState()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        // Verify that manual render mode change updates transition state
        Assert.Contains("const transitionState = renderModeTransitionState[canvasId];", source, StringComparison.Ordinal);
        Assert.Contains("if (mode === 'realistic') {", source, StringComparison.Ordinal);
        Assert.Contains("targetRenderModes[canvasId] = mode;", source, StringComparison.Ordinal);
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
        Assert.Contains("inputByteCount: result?.inputByteCount ?? null", source, StringComparison.Ordinal);
        Assert.Contains("inputTriangleCount: result?.inputTriangleCount ?? null", source, StringComparison.Ordinal);
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

    [Fact]
    public void KeyLight_PositionDerivedFromDirectionNotHardcoded()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        // The helper function must exist and compute position from direction vector.
        Assert.Contains("function _computeKeyLightPosition(dirConfig, modelCenter, dist)", source, StringComparison.Ordinal);
        Assert.Contains("dirConfig.x / mag", source, StringComparison.Ordinal);
        Assert.Contains("dirConfig.y / mag", source, StringComparison.Ordinal);
        Assert.Contains("dirConfig.z / mag", source, StringComparison.Ordinal);

        // Both the initial-load and the theme-switch path must delegate to it.
        Assert.Contains("key.position = _computeKeyLightPosition(_lc.key.direction, meshCenters[canvasId], dist);", source, StringComparison.Ordinal);
        Assert.Contains("key.position = _computeKeyLightPosition(_lc.key.direction, mc, dist);", source, StringComparison.Ordinal);

        // The old hardcoded formula that ignored the light direction must be gone.
        Assert.DoesNotContain("mc.x - dist, mc.y - dist, bb.max.z + dist", source, StringComparison.Ordinal);
        Assert.DoesNotContain("meshCenters[canvasId].x - dist, meshCenters[canvasId].y - dist, finalBb.max.z + dist", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ShadowCatcher_ActiveLightSetToKeyOnInitialLoad()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        // ShadowOnlyMaterial must name the key light so it picks the correct shadow
        // generator instead of falling through to the first scene light (hemi, no shadows).
        Assert.Contains("mat.activeLight  = key;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ShadowCatcher_ActiveLightUpdatedAfterThemeSwitch()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");

        // After a theme switch the old key light is disposed and a new one created.
        // The shadow catcher material must be updated to reference the new key light.
        Assert.Contains("catcherOnSwitch.material.activeLight = key;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ShadowGenerator_UsesPcfNotPcss()
    {
        var source = ViewerScript.ReplaceLineEndings("\n");
        var block  = ExtractBlock(source, "function configureSoftShadowGenerator(");

        // PCF (Percentage Closer Filtering) must be enabled and PCSS
        // (Contact Hardening Shadow) must be explicitly disabled.
        // These two modes are mutually exclusive in BabylonJS — the last
        // assignment wins — so both lines must be present and PCF must come
        // first (PCSS override is impossible when PCSS is explicitly false).
        Assert.Contains("shadowGenerator.usePercentageCloserFiltering   = true;", block, StringComparison.Ordinal);
        Assert.Contains("shadowGenerator.useContactHardeningShadow      = false;", block, StringComparison.Ordinal);

        // Guard against PCSS being silently re-enabled anywhere in the function
        // (check assignment forms only, not comment mentions of the property name).
        Assert.DoesNotContain("shadowGenerator.useContactHardeningShadow      = true", block, StringComparison.Ordinal);
        Assert.DoesNotContain("shadowGenerator.useContactHardeningShadow = true", block, StringComparison.Ordinal);

        // PCF assignment must appear BEFORE the PCSS-disable assignment so the ordering
        // is self-documenting and no future edit can swap them accidentally.
        // Search for "shadowGenerator.useX" to skip any comment lines that mention the property names.
        var pcfIndex  = block.IndexOf("shadowGenerator.usePercentageCloserFiltering", StringComparison.Ordinal);
        var pcssIndex = block.IndexOf("shadowGenerator.useContactHardeningShadow", StringComparison.Ordinal);
        Assert.True(pcfIndex < pcssIndex,
            "usePercentageCloserFiltering must appear before useContactHardeningShadow in configureSoftShadowGenerator");
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
        => ExtractExpression(source, "const renderMode =", "const cameraMode =");

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
