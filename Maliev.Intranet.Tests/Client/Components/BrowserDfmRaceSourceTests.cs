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
            gracePeriodMs >= 15_000,
            "Browser DFM should get at least the viewer worker's 15s budget before starting the server DFM fallback.");
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
