using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class PartsListPanelTests : BunitContext, IAsyncLifetime
{
    public PartsListPanelTests()
    {
        Services.AddLogging();
        Services.AddMudServices();
        Services.AddSingleton(new HttpClient { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton<CurrencyService>();
        Services.AddSingleton(CreateFileTypesSettings());
        Services.AddSingleton(new UploadSettings());
        Services.AddSingleton<ThumbnailGenerationService>();

        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void PartsListPanel_WhenProcessCodeIsMachineCode_RendersHumanProcessName()
    {
        var parts = new List<PartViewModel>
        {
            new()
            {
                Name = "bracket.stl",
                ProcessCode = "CNC_MILL"
            }
        };

        var cut = Render<PartsListPanel>(parameters => parameters
            .Add(p => p.Title, "Test quote")
            .Add(p => p.Parts, parts));

        var badge = cut.Find(".plp-process-badge");

        Assert.Equal("CNC Milling", badge.TextContent);
        Assert.DoesNotContain("CNC_MILL", cut.Markup);
    }

    [Fact]
    public void PartsListPanel_AwaitingPreviewWithoutThumbnail_ShowsDefaultIndeterminateProgressInsteadOfCustomLoaderOrFallbackIcon()
    {
        var parts = new List<PartViewModel>
        {
            new()
            {
                FileId = Guid.NewGuid(),
                Name = "bracket.stl",
                AwaitingPreview = true
            }
        };

        var cut = Render<PartsListPanel>(parameters => parameters
            .Add(p => p.Title, "Test quote")
            .Add(p => p.Parts, parts));

        var progress = cut.Find(".plp-thumb .mud-progress-circular");

        Assert.Contains("mud-progress-indeterminate", progress.ClassList);
        Assert.Contains("mud-default-text", progress.ClassList);
        Assert.Contains("mud-progress-small", progress.ClassList);
        Assert.Empty(cut.FindAll(".plp-thumb .part-processing-loader"));
        Assert.Empty(cut.FindAll(".plp-thumb .mud-skeleton"));
        Assert.Empty(cut.FindAll(".plp-thumb .mud-icon-root"));
    }

    [Fact]
    public void PartsListPanel_QueuedUploadBeforeProgress_ShowsDefaultIndeterminateProgressInsteadOfCustomLoaderOrFallbackIcon()
    {
        var parts = new List<PartViewModel>
        {
            new()
            {
                Name = "bracket.stl",
                QueuedUpload = true
            }
        };

        var cut = Render<PartsListPanel>(parameters => parameters
            .Add(p => p.Title, "Test quote")
            .Add(p => p.Parts, parts));

        var progress = cut.Find(".plp-thumb .mud-progress-circular");

        Assert.Contains("mud-progress-indeterminate", progress.ClassList);
        Assert.Contains("mud-default-text", progress.ClassList);
        Assert.Contains("mud-progress-small", progress.ClassList);
        Assert.Empty(cut.FindAll(".plp-thumb .part-queue-loader"));
        Assert.Empty(cut.FindAll(".plp-thumb .mud-skeleton"));
        Assert.Empty(cut.FindAll(".plp-thumb .mud-icon-root"));
    }

    [Fact]
    public void PartsListPanel_UploadingBeforeFirstProgress_ShowsDeterminateUploadProgress()
    {
        var parts = new List<PartViewModel>
        {
            new()
            {
                Name = "bracket.stl",
                Uploading = true,
                ProgressPercent = 0
            }
        };

        var cut = Render<PartsListPanel>(parameters => parameters
            .Add(p => p.Title, "Test quote")
            .Add(p => p.Parts, parts));

        var progress = cut.Find(".plp-thumb .mud-progress-circular");

        Assert.Equal("0", progress.GetAttribute("aria-valuenow"));
        Assert.DoesNotContain("mud-progress-circular-indeterminate", progress.ClassList);
        Assert.Empty(cut.FindAll(".plp-thumb .mud-skeleton"));
        Assert.Empty(cut.FindAll(".plp-thumb .mud-icon-root"));
    }

    [Fact]
    public void PartsListPanel_AwaitingPreviewWithUploadProgress_ShowsDeterminateProgress()
    {
        var parts = new List<PartViewModel>
        {
            new()
            {
                FileId = Guid.NewGuid(),
                Name = "bracket.stl",
                AwaitingPreview = true,
                ProgressPercent = 100
            }
        };

        var cut = Render<PartsListPanel>(parameters => parameters
            .Add(p => p.Title, "Test quote")
            .Add(p => p.Parts, parts));

        var progress = cut.Find(".plp-thumb .mud-progress-circular");

        Assert.Contains("mud-progress-indeterminate", progress.ClassList);
        Assert.Contains("mud-default-text", progress.ClassList);
        Assert.Contains("mud-progress-small", progress.ClassList);
        Assert.Empty(cut.FindAll(".plp-thumb .part-processing-loader"));
    }

    [Fact]
    public void PartsListPanel_UploadingAndAwaitingPreview_ShowsLiveUploadProgress()
    {
        var parts = new List<PartViewModel>
        {
            new()
            {
                FileId = Guid.NewGuid(),
                Name = "bracket.stl",
                Uploading = true,
                AwaitingPreview = true,
                ProgressPercent = 44
            }
        };

        var cut = Render<PartsListPanel>(parameters => parameters
            .Add(p => p.Title, "Test quote")
            .Add(p => p.Parts, parts));

        var progress = cut.Find(".plp-thumb .mud-progress-circular");

        Assert.Equal("44", progress.GetAttribute("aria-valuenow"));
        Assert.DoesNotContain("mud-progress-circular-indeterminate", progress.ClassList);
    }

    [Fact]
    public void PartsListPanel_SelectedCustomer_RendersFullProfileSummary()
    {
        var customer = new CustomerSummaryDto
        {
            Id = Guid.NewGuid(),
            Name = "Mayuree Nguyen",
            CompanyName = "Axion Robotics",
            Tier = "Gold",
            Email = "mayuree@example.com",
            Mobile = "+66 81 234 5678",
            ProfileImageUrl = "https://images.example/mayuree.png",
        };

        var cut = Render<PartsListPanel>(parameters => parameters
            .Add(p => p.Title, "Project 2026-06-10")
            .Add(p => p.Parts, [])
            .Add(p => p.SelectedCustomer, customer));

        var card = cut.Find(".plp-customer-card");

        Assert.Equal("https://images.example/mayuree.png", card.QuerySelector(".plp-customer-avatar")?.GetAttribute("src"));
        Assert.Contains("Mayuree Nguyen", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Axion Robotics", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Gold", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("mayuree@example.com", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("+66 81 234 5678", card.TextContent, StringComparison.Ordinal);
    }

    private static FileTypesSettings CreateFileTypesSettings() => new()
    {
        ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".stl", ".step", ".3mf", ".obj" },
        DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".dxf", ".dwg" },
        ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" },
        OfficeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".doc", ".docx", ".xls", ".xlsx" },
        ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".zip", ".rar", ".7z" },
        DrawingExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".dxf", ".dwg", ".png", ".jpg" },
        SupplementaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".doc", ".docx", ".xls", ".xlsx", ".zip" }
    };
}
