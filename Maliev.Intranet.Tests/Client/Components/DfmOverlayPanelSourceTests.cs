namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Source-level regressions for DFM overlay messaging.
/// </summary>
public sealed class DfmOverlayPanelSourceTests
{
    [Fact]
    public void DfmOverlayPanel_DoesNotRenderAllClearNoIssuesBanner()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "DfmOverlayPanel.razor")
            .ReplaceLineEndings("\n");

        Assert.DoesNotContain("All Clear", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("No Issues Found", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dfm-overlay--all-clear", source, StringComparison.Ordinal);
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
