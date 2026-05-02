using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
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

        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void PartsListPanel_AwaitingPreviewWithoutThumbnail_ShowsSpinnerInsteadOfFallbackIcon()
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

        Assert.NotEmpty(cut.FindAll(".plp-thumb .mud-progress-circular"));
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

        Assert.Equal("100", progress.GetAttribute("aria-valuenow"));
        Assert.DoesNotContain("mud-progress-circular-indeterminate", progress.ClassList);
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
