namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Source-level regressions for browser-first DFM races against server fallback failures.
/// </summary>
public sealed class BrowserDfmRaceSourceTests
{
    [Fact]
    public void BrowserDfmGracePeriodCoversLocalWorkerBudgetBeforeServerFallback()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "BrowserDfmReportSync.cs")
            .ReplaceLineEndings("\n");
        var match = System.Text.RegularExpressions.Regex.Match(
            source,
            @"BrowserDfmGracePeriodMs\s*=\s*(?<value>[\d_]+)");

        Assert.True(match.Success, "BrowserDfmReportSync must define an explicit browser DFM grace period.");

        var gracePeriodMs = int.Parse(
            match.Groups["value"].Value.Replace("_", string.Empty, StringComparison.Ordinal),
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(
            gracePeriodMs >= 25_000,
            "Browser DFM should cover the manifest's 20s desktop worker budget plus callback margin before starting the server DFM fallback.");
    }

    [Fact]
    public void BrowserDfmCurrentReportUsesNormalizedProcessCodes()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "BrowserDfmReportSync.cs")
            .ReplaceLineEndings("\n");

        Assert.Contains("ProcessCodeNormalizer.Equals(part.ProcessCode, processCode)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string.Equals(part.ProcessCode, processCode", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserDfmTerminalLocalAttemptEndsGraceWaitBeforeServerFallback()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "BrowserDfmReportSync.cs")
            .ReplaceLineEndings("\n");
        var waitBlock = ExtractBlock(source, "internal static async Task<bool> WaitForCurrentReportAsync");

        Assert.Contains("HasTerminalLocalAttempt(part, processCode)", source, StringComparison.Ordinal);
        Assert.True(
            waitBlock.IndexOf("HasTerminalLocalAttempt(part, processCode)", StringComparison.Ordinal)
            < waitBlock.IndexOf("Task.Delay", StringComparison.Ordinal),
            "BrowserDfmReportSync must stop waiting as soon as the browser reports a terminal local runtime attempt.");
        Assert.Contains("MarkTerminalLocalAttempt", source, StringComparison.Ordinal);
        Assert.Contains("ClearTerminalLocalAttempt", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserDfmStartedLocalAttemptExtendsServerFallbackWindow()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "BrowserDfmReportSync.cs")
            .ReplaceLineEndings("\n");
        var waitBlock = ExtractBlock(source, "internal static async Task<bool> WaitForCurrentReportAsync");

        Assert.Contains("MarkLocalAttemptStarted", source, StringComparison.Ordinal);
        Assert.Contains("HasActiveLocalAttempt(part, processCode)", waitBlock, StringComparison.Ordinal);
        Assert.Contains("GetActiveLocalAttemptDeadline(part, processCode)", waitBlock, StringComparison.Ordinal);
        Assert.True(
            waitBlock.IndexOf("GetActiveLocalAttemptDeadline(part, processCode)", StringComparison.Ordinal)
            < waitBlock.IndexOf("DateTimeOffset.UtcNow >= effectiveDeadline", StringComparison.Ordinal),
            "Server DFM fallback must account for a browser worker that started after the initial grace window began.");
    }

    [Fact]
    public void ModelViewer_NotifiesBlazorWhenBrowserLocalDfmStarts()
    {
        var viewer = ReadRepoFile("Maliev.Intranet.Client", "Components", "ModelViewer.razor")
            .ReplaceLineEndings("\n");
        var script = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "js", "part-viewer.js")
            .ReplaceLineEndings("\n");

        Assert.Contains("NotifyLocalGeometryRuntimeStarted", viewer, StringComparison.Ordinal);
        Assert.Contains("notifyLocalAdvisoryStartedDotNet", script, StringComparison.Ordinal);
        Assert.True(
            script.IndexOf("await notifyLocalAdvisoryStartedDotNet", StringComparison.Ordinal)
            < script.IndexOf("const result = await enqueueLocalAdvisoryWorker", StringComparison.Ordinal),
            "The browser must tell Blazor that local DFM has started before awaiting worker completion.");
    }

    [Fact]
    public void BrowserLocalDfmStartCarriesWorkloadIntoPartState()
    {
        var runtimeModels = ReadRepoFile("Maliev.Intranet.Client", "Components", "LocalGeometryRuntimeResult.cs")
            .ReplaceLineEndings("\n");
        var partModel = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartViewModel.cs")
            .ReplaceLineEndings("\n");
        var sync = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "BrowserDfmReportSync.cs")
            .ReplaceLineEndings("\n");
        var project = ReadRepoFile("Maliev.Intranet.Client", "Pages", "ProjectNew.razor.cs")
            .ReplaceLineEndings("\n");

        Assert.Contains("public long? InputByteCount { get; set; }", runtimeModels, StringComparison.Ordinal);
        Assert.Contains("public long? InputTriangleCount { get; set; }", runtimeModels, StringComparison.Ordinal);
        Assert.Contains("public long? LocalDfmRuntimeInputByteCount { get; set; }", partModel, StringComparison.Ordinal);
        Assert.Contains("public long? LocalDfmRuntimeInputTriangleCount { get; set; }", partModel, StringComparison.Ordinal);
        Assert.Contains("part.LocalDfmRuntimeInputByteCount = inputByteCount", sync, StringComparison.Ordinal);
        Assert.Contains("part.LocalDfmRuntimeInputTriangleCount = inputTriangleCount", sync, StringComparison.Ordinal);
        Assert.Contains("completion.Result.InputByteCount", project, StringComparison.Ordinal);
        Assert.Contains("completion.Result.InputTriangleCount", project, StringComparison.Ordinal);
    }

    [Fact]
    public void DfmOverlayDistinguishesBrowserLocalWorkFromServerFallback()
    {
        var detail = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartDetailCard.razor")
            .ReplaceLineEndings("\n");
        var overlay = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "DfmOverlayPanel.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains("AnalyzingMessage=\"@GetDfmAnalyzingMessage()\"", detail, StringComparison.Ordinal);
        Assert.Contains("Running local DFM on this device", detail, StringComparison.Ordinal);
        Assert.Contains("LocalDfmRuntimeInputTriangleCount", detail, StringComparison.Ordinal);
        Assert.Contains("[Parameter] public string? AnalyzingMessage { get; set; }", overlay, StringComparison.Ordinal);
        Assert.Contains("AnalyzingMessage ?? \"Analyzing your model", overlay, StringComparison.Ordinal);
    }

    [Fact]
    public void DfmOverlayStopsAnalyzingWhenBrowserLocalDfmTerminalStateExists()
    {
        var detail = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartDetailCard.razor")
            .ReplaceLineEndings("\n");
        var analyzingBlock = ExtractBlock(detail, "private bool IsDfmAnalyzing");

        Assert.Contains("!BrowserDfmReportSync.HasTerminalLocalAttempt(Part, Part.ProcessCode ?? string.Empty)", analyzingBlock, StringComparison.Ordinal);
        Assert.Contains("&& !DfmUnavailable", analyzingBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectNew_StatusWatchdogBoundsMissingAnalysisStatusWithoutViewerUrlFanout()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "ProjectNew.razor.cs")
            .ReplaceLineEndings("\n");
        var fetchBlock = ExtractBlock(source, "private async Task FetchCurrentStatusAsync");
        var notFoundBranch = ExtractBlock(fetchBlock, "if (statusResponse.StatusCode == System.Net.HttpStatusCode.NotFound)");

        Assert.Contains("MissingAnalysisStatusMaxPolls", source, StringComparison.Ordinal);
        Assert.Contains("missingPolls >= MissingAnalysisStatusMaxPolls", notFoundBranch, StringComparison.Ordinal);
        Assert.Contains("MarkAnalysisStatusUnavailable(part);", notFoundBranch, StringComparison.Ordinal);
        Assert.Contains("return;", notFoundBranch, StringComparison.Ordinal);
        Assert.True(
            fetchBlock.IndexOf("statusResponse.StatusCode == System.Net.HttpStatusCode.NotFound", StringComparison.Ordinal)
            < fetchBlock.IndexOf("ResolveViewerUrlAsync(part)", StringComparison.Ordinal),
            "Missing analysis-status rows must be bounded before the watchdog fans out into repeated viewer-url calls.");
    }

    [Fact]
    public void ModelViewer_DoesNotRenderSecondLocalDfmPanelWhenBlazorHandlesRuntimeState()
    {
        var script = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "js", "part-viewer.js")
            .ReplaceLineEndings("\n");

        Assert.Contains("shouldRenderLocalAdvisoryPanel(options)", script, StringComparison.Ordinal);
        Assert.Contains("const renderLocalPanel = shouldRenderLocalAdvisoryPanel(options);", script, StringComparison.Ordinal);
        Assert.DoesNotContain("\n    renderLocalAdvisoryStatus(canvasId, 'pending');", script, StringComparison.Ordinal);
        Assert.Contains("if (renderLocalPanel) renderLocalAdvisoryStatus(canvasId, 'pending');", script, StringComparison.Ordinal);
    }

    [Fact]
    public void PartDetailCard_DefersBrowserLocalDfmUntilViewerReferenceExists()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartDetailCard.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains("private string? _pendingLocalDfmProcessCode;", source, StringComparison.Ordinal);
        Assert.Contains("private bool _viewerConfigurationPushPending;", source, StringComparison.Ordinal);
        Assert.Contains("protected override async Task OnAfterRenderAsync", source, StringComparison.Ordinal);
        Assert.Contains("_pendingLocalDfmProcessCode = Part.ProcessCode;", source, StringComparison.Ordinal);
        Assert.Contains("await _modelViewer.RunLocalGeometryRuntimeAsync(localDfmProcessCode);", source, StringComparison.Ordinal);

        var pushMethod = ExtractBlock(source, "private async Task PushViewerConfiguratorStateAsync");
        Assert.Contains("if (_modelViewer == null)", pushMethod, StringComparison.Ordinal);
        Assert.True(
            pushMethod.IndexOf("if (_modelViewer == null)", StringComparison.Ordinal)
            < pushMethod.IndexOf("_hasPushedInitialMaterialConfig = true;", StringComparison.Ordinal),
            "PartDetailCard must not mark the initial viewer config push complete before the ModelViewer @ref exists.");
    }

    [Fact]
    public void ProjectNew_DfmGoneResponseDoesNotOverrideCurrentBrowserReport()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "ProjectNew.razor.cs")
            .ReplaceLineEndings("\n");
        var goneBranch = ExtractBlock(source, "else if (response.StatusCode == System.Net.HttpStatusCode.Gone)");

        Assert.Contains("BrowserDfmReportSync.HasCurrentReport(part, processCode)", goneBranch, StringComparison.Ordinal);
        Assert.True(
            goneBranch.IndexOf("BrowserDfmReportSync.HasCurrentReport(part, processCode)", StringComparison.Ordinal)
            < goneBranch.IndexOf("part.AnalysisErrorCode = \"FILE_MISSING\";", StringComparison.Ordinal),
            "ProjectNew must keep a current browser DFM report instead of stamping FILE_MISSING after a late 410.");
    }

    [Fact]
    public void ProjectNew_DfmGoneResponseDoesNotOverrideActiveBrowserAttempt()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "ProjectNew.razor.cs")
            .ReplaceLineEndings("\n");
        var goneBranch = ExtractBlock(source, "else if (response.StatusCode == System.Net.HttpStatusCode.Gone)");

        Assert.Contains("BrowserDfmReportSync.HasActiveLocalAttempt(part, processCode)", goneBranch, StringComparison.Ordinal);
        Assert.True(
            goneBranch.IndexOf("BrowserDfmReportSync.HasActiveLocalAttempt(part, processCode)", StringComparison.Ordinal)
            < goneBranch.IndexOf("part.AnalysisErrorCode = \"FILE_MISSING\";", StringComparison.Ordinal),
            "ProjectNew must not stamp FILE_MISSING while browser-first local DFM is still running for the current process.");
    }

    [Fact]
    public void PartConfigSidebar_DfmGoneResponseDoesNotOverrideCurrentBrowserReport()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartConfigSidebar.razor.cs")
            .ReplaceLineEndings("\n");
        var goneBranch = ExtractBlock(source, "else if (response.StatusCode == System.Net.HttpStatusCode.Gone)");

        Assert.Contains("BrowserDfmReportSync.HasCurrentReport(part, process.Code)", goneBranch, StringComparison.Ordinal);
        Assert.True(
            goneBranch.IndexOf("BrowserDfmReportSync.HasCurrentReport(part, process.Code)", StringComparison.Ordinal)
            < goneBranch.IndexOf("part.AnalysisErrorCode = \"FILE_MISSING\";", StringComparison.Ordinal),
            "PartConfigSidebar must keep a current browser DFM report instead of stamping FILE_MISSING after a late 410.");
    }

    [Fact]
    public void PartConfigSidebar_DfmGoneResponseDoesNotOverrideActiveBrowserAttempt()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartConfigSidebar.razor.cs")
            .ReplaceLineEndings("\n");
        var goneBranch = ExtractBlock(source, "else if (response.StatusCode == System.Net.HttpStatusCode.Gone)");

        Assert.Contains("BrowserDfmReportSync.HasActiveLocalAttempt(part, process.Code)", goneBranch, StringComparison.Ordinal);
        Assert.True(
            goneBranch.IndexOf("BrowserDfmReportSync.HasActiveLocalAttempt(part, process.Code)", StringComparison.Ordinal)
            < goneBranch.IndexOf("part.AnalysisErrorCode = \"FILE_MISSING\";", StringComparison.Ordinal),
            "PartConfigSidebar must not stamp FILE_MISSING while browser-first local DFM is still running for the current process.");
    }

    [Fact]
    public void ProjectNew_BrowserPrimaryPolicyPreventsInteractiveServerDfmFallback()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "ProjectNew.razor.cs")
            .ReplaceLineEndings("\n");
        var analyzeBlock = ExtractBlock(source, "private async Task RunProcessDfmAnalysisAsync");

        Assert.Contains("interactiveServerDfmFallbackForBrowserPrimaryUploads", source, StringComparison.Ordinal);
        Assert.Contains("ShouldRunInteractiveServerDfmFallbackAsync(part)", analyzeBlock, StringComparison.Ordinal);
        Assert.Contains("MarkBrowserPrimaryLocalDfmUnavailable(part, processCode", analyzeBlock, StringComparison.Ordinal);
        Assert.True(
            analyzeBlock.IndexOf("ShouldRunInteractiveServerDfmFallbackAsync(part)", StringComparison.Ordinal)
            < analyzeBlock.IndexOf("Http.PostAsJsonAsync", StringComparison.Ordinal),
            "ProjectNew must honor browser-primary runtime policy before posting to the GeometryService server DFM fallback endpoint.");
    }

    [Fact]
    public void ProjectNew_BrowserPrimaryUploadSkipsServerStatusWatchdogAfterLocalViewer()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Pages", "ProjectNew.razor.cs")
            .ReplaceLineEndings("\n");
        var uploadBlock = ExtractBlock(source, "private async Task UploadSingleProjectFileAsync");

        Assert.Contains("TryCompleteBrowserPrimaryViewerLocallyAsync(part)", uploadBlock, StringComparison.Ordinal);
        Assert.Contains("StartStatusWatchdog(part, completedUpload.StoragePath)", uploadBlock, StringComparison.Ordinal);
        Assert.True(
            uploadBlock.IndexOf("TryCompleteBrowserPrimaryViewerLocallyAsync(part)", StringComparison.Ordinal)
            < uploadBlock.IndexOf("StartStatusWatchdog(part, completedUpload.StoragePath)", StringComparison.Ordinal),
            "ProjectNew must try the browser-local viewer path before starting analysis-status polling.");
        Assert.Contains("if (!completedLocally)", uploadBlock, StringComparison.Ordinal);
        var localViewerBlock = ExtractBlock(source, "private async Task<bool> TryCompleteBrowserPrimaryViewerLocallyAsync");
        Assert.Contains("part.AwaitingPreview = false;", localViewerBlock, StringComparison.Ordinal);
        Assert.Contains("part.StatusText = \"Ready\";", localViewerBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void PartConfigSidebar_BrowserPrimaryPolicyPreventsInteractiveServerDfmFallback()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartConfigSidebar.razor.cs")
            .ReplaceLineEndings("\n");
        var analyzeBlock = ExtractBlock(source, "private async Task AnalyzeProcessForDfm");

        Assert.Contains("[Parameter] public bool BrowserPrimaryServerDfmFallbackEnabled { get; set; }", source, StringComparison.Ordinal);
        Assert.Contains("ShouldRunInteractiveServerDfmFallback(part)", analyzeBlock, StringComparison.Ordinal);
        Assert.Contains("MarkBrowserPrimaryLocalDfmUnavailable(part, process.Code", analyzeBlock, StringComparison.Ordinal);
        Assert.True(
            analyzeBlock.IndexOf("ShouldRunInteractiveServerDfmFallback(part)", StringComparison.Ordinal)
            < analyzeBlock.IndexOf("Http.PostAsJsonAsync", StringComparison.Ordinal),
            "PartConfigSidebar must honor browser-primary runtime policy before posting to the GeometryService server DFM fallback endpoint.");
    }

    [Fact]
    public void PartListRow_ForwardsBrowserPrimaryPolicyToExpandedSidebar()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartListRow.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains("[Parameter] public bool BrowserPrimaryServerDfmFallbackEnabled { get; set; } = true;", source, StringComparison.Ordinal);
        Assert.Contains("BrowserPrimaryServerDfmFallbackEnabled=\"@BrowserPrimaryServerDfmFallbackEnabled\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PartConfigSidebar_PublishesDfmStateForEquivalentProcessCodes()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartConfigSidebar.razor.cs")
            .ReplaceLineEndings("\n");

        Assert.Contains("ProcessCodeNormalizer.Equals(part.ProcessCode, process.Code)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("part.ProcessCode == process.Code", source, StringComparison.Ordinal);
    }

    private static string ExtractBlock(string source, string start)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"Unable to locate block start: {start}");

        var depth = 0;
        var opened = false;
        for (var i = startIndex; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
                opened = true;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (opened && depth == 0)
                    return source[startIndex..(i + 1)];
            }
        }

        throw new InvalidDataException($"Unable to extract block starting at: {start}");
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
