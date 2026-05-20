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
        Assert.DoesNotContain("::deep(", styles, StringComparison.Ordinal);
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

    [Fact]
    public void ActiveToggle_RendersInEditorHeader()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor.css");

        var headerIndex = page.IndexOf("chatbot-instruction-editor-header", StringComparison.Ordinal);
        var stackIndex = page.IndexOf("<MudStack Spacing=\"2\">", StringComparison.Ordinal);

        Assert.True(headerIndex >= 0, "The editor should have a dedicated header.");
        Assert.True(stackIndex >= 0, "The editor should keep the field stack.");
        Assert.True(headerIndex < stackIndex, "The active control belongs above the editor fields.");
        Assert.Contains("chatbot-instruction-active-control", page, StringComparison.Ordinal);
        Assert.Contains("chatbot-instruction-active-switch", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Active\"", page, StringComparison.Ordinal);
        Assert.Contains("border-radius: 999px;", styles, StringComparison.Ordinal);
        Assert.Contains(".chatbot-instruction-active-control ::deep .chatbot-instruction-active-switch", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void InstructionTextFields_ShowCountsAndLengthWarnings()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor.css");
        var dto = ReadRepoFile("Maliev.Intranet.Shared", "Dtos", "ChatDtos.cs");
        var mutationRequest = dto[dto.IndexOf("public class BffSystemInstructionMutationRequest", StringComparison.Ordinal)..];

        Assert.Contains("BffSystemInstructionLimits", dto, StringComparison.Ordinal);
        Assert.Contains("public const int InstructionTextWarningLength = 4000;", dto, StringComparison.Ordinal);
        Assert.Contains("public const int InstructionTextMaxLength = 5000;", dto, StringComparison.Ordinal);
        Assert.Contains("[StringLength(BffSystemInstructionLimits.NameMaxLength, MinimumLength = 1)]", mutationRequest, StringComparison.Ordinal);
        Assert.Contains("[StringLength(BffSystemInstructionLimits.TopicKeyMaxLength)]", mutationRequest, StringComparison.Ordinal);
        Assert.Contains("BffSystemInstructionLimits.InstructionTextMaxLength", mutationRequest, StringComparison.Ordinal);
        Assert.Contains("MinimumLength = BffSystemInstructionLimits.InstructionTextMinLength", mutationRequest, StringComparison.Ordinal);
        Assert.Contains("FormatInstructionTextCount(_form.PersonaDefinition)", page, StringComparison.Ordinal);
        Assert.Contains("FormatInstructionTextCount(_form.BusinessConstraints)", page, StringComparison.Ordinal);
        Assert.Contains("InstructionTextLengthMessage(_form.PersonaDefinition)", page, StringComparison.Ordinal);
        Assert.Contains("InstructionTextLengthMessage(_form.BusinessConstraints)", page, StringComparison.Ordinal);
        Assert.Contains("HasInstructionTextLengthError", page, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(_saving || _refining || HasInstructionTextLengthError)\"", page, StringComparison.Ordinal);
        Assert.Contains("chatbot-instruction-character-count", styles, StringComparison.Ordinal);
        Assert.Contains(".chatbot-instruction-character-count.near-limit", styles, StringComparison.Ordinal);
        Assert.Contains(".chatbot-instruction-character-count.over-limit", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void InstructionEditor_OffersAiRefinement()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor.css");
        var dto = ReadRepoFile("Maliev.Intranet.Shared", "Dtos", "ChatDtos.cs");
        var controller = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "ChatController.cs");
        var serviceClient = ReadRepoFile("Maliev.Intranet.Bff", "Clients", "ChatbotServiceClient.cs");

        Assert.Contains("Improve with AI", page, StringComparison.Ordinal);
        Assert.Contains("OnClick=\"RefineInstructionAsync\"", page, StringComparison.Ordinal);
        Assert.Contains("api/v1/chat/instructions/refine", page, StringComparison.Ordinal);
        Assert.Contains("_refining", page, StringComparison.Ordinal);
        Assert.Contains("BffSystemInstructionRefinementRequest", dto, StringComparison.Ordinal);
        Assert.Contains("BffSystemInstructionRefinementResponse", dto, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(\"instructions/refine\")]", controller, StringComparison.Ordinal);
        Assert.Contains("RefineSystemInstructionAsync", serviceClient, StringComparison.Ordinal);
        Assert.Contains("chatbot-instruction-ai-summary", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void PriorityEditor_UsesBoundedLevelButtons()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor.css");
        var dto = ReadRepoFile("Maliev.Intranet.Shared", "Dtos", "ChatDtos.cs");
        var mutationRequest = dto[dto.IndexOf("public class BffSystemInstructionMutationRequest", StringComparison.Ordinal)..];

        Assert.DoesNotContain("MudNumericField @bind-Value=\"_form.Priority\"", page, StringComparison.Ordinal);
        Assert.Contains("role=\"radiogroup\"", page, StringComparison.Ordinal);
        Assert.Contains("PriorityOptions", page, StringComparison.Ordinal);
        Assert.Contains("SetPriority(priority)", page, StringComparison.Ordinal);
        Assert.Contains("1 Low", page, StringComparison.Ordinal);
        Assert.Contains("5 High", page, StringComparison.Ordinal);
        Assert.Contains("ClampPriority(instruction.Priority)", page, StringComparison.Ordinal);
        Assert.Contains("ClampPriority(priority)", page, StringComparison.Ordinal);
        Assert.Contains("chatbot-priority-button", styles, StringComparison.Ordinal);
        Assert.Contains("chatbot-priority-button.selected", styles, StringComparison.Ordinal);
        Assert.Contains("public const int PriorityMin = 1;", dto, StringComparison.Ordinal);
        Assert.Contains("public const int PriorityMax = 5;", dto, StringComparison.Ordinal);
        Assert.Contains("[Range(BffSystemInstructionLimits.PriorityMin, BffSystemInstructionLimits.PriorityMax)]", mutationRequest, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkdownImporter_ExplainsPromptAndConstraintMapping()
    {
        var page = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor");
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Pages", "Admin", "ChatbotInstructions.razor.css");

        Assert.DoesNotContain("Upload Markdown with frontmatter such as", page, StringComparison.Ordinal);
        Assert.Contains("chatbot-import-panel", page, StringComparison.Ordinal);
        Assert.Contains("Markdown import", page, StringComparison.Ordinal);
        Assert.Contains("Body -> System prompt", page, StringComparison.Ordinal);
        Assert.Contains("## Business Constraints -> Business constraints", page, StringComparison.Ordinal);
        Assert.Contains("chatbot-prompt-import", page, StringComparison.Ordinal);
        Assert.Contains("_importedPromptFileName", page, StringComparison.Ordinal);
        Assert.Contains("chatbot-import-input", styles, StringComparison.Ordinal);
        Assert.Contains("chatbot-import-button", styles, StringComparison.Ordinal);
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
