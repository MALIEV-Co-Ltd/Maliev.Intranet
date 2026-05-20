using System.Runtime.CompilerServices;

namespace Maliev.Intranet.Tests.Client.Pages;

public class ChatbotInstructionsSourceTests
{
    [Fact]
    public void FilterToolbar_UsesCompactControls()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor.css");

        Assert.Contains("chatbot-instruction-filter-toolbar", page, StringComparison.Ordinal);
        Assert.Contains("Class=\"chatbot-filter-search\"", page, StringComparison.Ordinal);
        Assert.Contains("Class=\"chatbot-filter-category\"", page, StringComparison.Ordinal);
        Assert.Contains("Placeholder=\"Topic/profile key\"", page, StringComparison.Ordinal);
        Assert.Contains("All categories", page, StringComparison.Ordinal);
        Assert.DoesNotContain("flex: 1 1 260px", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Topic/profile key\"", page, StringComparison.Ordinal);
        Assert.Contains("flex: 0 1 320px;", styles, StringComparison.Ordinal);
        Assert.Contains("width: 160px;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionColumn_UsesExplicitRazorExpression()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor");

        Assert.DoesNotContain("v@instruction.Version", page, StringComparison.Ordinal);
        Assert.Contains("@FormatInstructionVersion(instruction.Version)", page, StringComparison.Ordinal);
        Assert.Contains("private static string FormatInstructionVersion(int version)", page, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var startDirectories = new List<string>();
        var configuredRoot = Environment.GetEnvironmentVariable("MALIEV_INTRANET_REPO_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            startDirectories.Add(configuredRoot);
        }

        startDirectories.Add(GetSourceDirectory());
        startDirectories.Add(AppContext.BaseDirectory);
        startDirectories.Add(Directory.GetCurrentDirectory());

        foreach (var startDirectory in startDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }

    private static string GetSourceDirectory([CallerFilePath] string sourceFile = "") => Path.GetDirectoryName(sourceFile) ?? Directory.GetCurrentDirectory();
}
