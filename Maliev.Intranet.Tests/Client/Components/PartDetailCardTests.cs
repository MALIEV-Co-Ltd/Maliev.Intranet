using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

// Module URL used by ModelViewer when it imports the JS viewer.
file static class ViewerModuleUrl
{
    public const string Url = "./js/part-viewer.js";
}

public class PartDetailCardTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<ILogger<LayoutService>> _loggerMock = new();

    public PartDetailCardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new LayoutService(JSInterop.JSRuntime, _loggerMock.Object));
        Services.AddSingleton(new FileTypesSettings
        {
            ThreeDExtensions = [".stl", ".step", ".stp", ".3mf", ".obj"],
            DocumentExtensions = [".pdf", ".dxf", ".dwg"],
            ImageExtensions = [".png", ".jpg", ".jpeg", ".webp"],
            OfficeExtensions = [".doc", ".docx", ".xls", ".xlsx"],
            ArchiveExtensions = [".zip", ".rar", ".7z"],
            DrawingExtensions = [".pdf", ".dxf", ".dwg", ".png", ".jpg"],
            SupplementaryExtensions = [".png", ".jpg", ".doc", ".docx", ".zip"],
        });
        Services.AddSingleton(new System.Net.Http.HttpClient());
        Render<MudBlazor.MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    private static PartViewModel MakePartWithViewer(string? processCode)
    {
        return new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "clip lock-v.2.stl",
            ViewerUrl = "https://example.com/viewer.glb",
            StoragePath = "projects/test/clip_lock.stl",
            ProcessCode = processCode,
            DfmReport = null,
            DfmAcknowledged = false,
            DfmAnalysisTimedOut = false,
        };
    }

    [Fact]
    public void DfmOverlay_ShouldNotShowAnalyzing_WhenNoProcessSelected()
    {
        var part = MakePartWithViewer(processCode: null);
        var cut = Render<PartDetailCard>(p => p
            .Add(x => x.Part, part)
            .Add(x => x.TempProjectId, Guid.NewGuid()));

        Assert.DoesNotContain("Analyzing your model", cut.Markup);
    }

    [Fact]
    public void DfmOverlay_ShouldNotShowAnalyzing_WhenProcessCodeIsEmpty()
    {
        var part = MakePartWithViewer(processCode: "");
        var cut = Render<PartDetailCard>(p => p
            .Add(x => x.Part, part)
            .Add(x => x.TempProjectId, Guid.NewGuid()));

        Assert.DoesNotContain("Analyzing your model", cut.Markup);
    }

    [Fact]
    public void DfmOverlay_ShouldShowAnalyzing_WhenProcessSelectedAndDfmPending()
    {
        var part = MakePartWithViewer(processCode: "FDM");
        var cut = Render<PartDetailCard>(p => p
            .Add(x => x.Part, part)
            .Add(x => x.TempProjectId, Guid.NewGuid()));

        Assert.Contains("Analyzing your model", cut.Markup);
    }

    // ── HandleToggleOverlay co-toggle tests ──────────────────────────────────

    private static PartViewModel MakePartWithOverlayUrls(Dictionary<string, string> overlayUrls)
    {
        return new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "test.stl",
            ViewerUrl = "https://example.com/model.glb",
            StoragePath = "projects/test/test.stl",
            ProcessCode = "FDM",
            DfmReport = null,
            DfmAcknowledged = false,
            DfmAnalysisTimedOut = false,
            OverlayUrls = overlayUrls,
        };
    }

    private static async Task InvokeHandleToggleOverlay(PartDetailCard instance, DfmIssue issue)
    {
        var method = typeof(PartDetailCard).GetMethod(
            "HandleToggleOverlay",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException("HandleToggleOverlay not found");
        await (Task)method.Invoke(instance, new object[] { issue })!;
    }

    [Fact]
    public async Task HandleToggleOverlay_CoTogglesSupportTower_WhenFdmOverhangHasCompanionUrl()
    {
        var moduleInterop = JSInterop.SetupModule(ViewerModuleUrl.Url);

        var part = MakePartWithOverlayUrls(new Dictionary<string, string>
        {
            ["FDM__overhang"]         = "https://example.com/overhang.glb",
            ["FDM__overhang_support"] = "https://example.com/overhang_support.glb",
        });

        var cut = Render<PartDetailCard>(p => p
            .Add(x => x.Part, part)
            .Add(x => x.TempProjectId, Guid.NewGuid()));

        var issue = new DfmIssue(
            Icon: "icon", Title: "Overhang", Description: "desc",
            Severity: DfmIssueSeverity.Warning,
            Category: "overhang",
            OverlayKey: "FDM__overhang",
            OverlayUrl: "https://example.com/overhang.glb");

        await InvokeHandleToggleOverlay(cut.Instance, issue);

        var toggleCalls = moduleInterop.Invocations
            .Where(i => i.Identifier == "toggleDfmOverlay")
            .ToList();
        Assert.Equal(2, toggleCalls.Count);
        // First call: primary overhang overlay
        Assert.Equal("FDM__overhang",         toggleCalls[0].Arguments[1]);
        // Second call: companion support-tower overlay
        Assert.Equal("FDM__overhang_support", toggleCalls[1].Arguments[1]);
        // Both calls share the same isActive flag
        Assert.Equal(toggleCalls[0].Arguments[3], toggleCalls[1].Arguments[3]);
    }

    [Fact]
    public async Task HandleToggleOverlay_DoesNotCoToggle_WhenCompanionUrlMissing()
    {
        var moduleInterop = JSInterop.SetupModule(ViewerModuleUrl.Url);

        // Only primary overlay URL present — companion key absent
        var part = MakePartWithOverlayUrls(new Dictionary<string, string>
        {
            ["FDM__overhang"] = "https://example.com/overhang.glb",
        });

        var cut = Render<PartDetailCard>(p => p
            .Add(x => x.Part, part)
            .Add(x => x.TempProjectId, Guid.NewGuid()));

        var issue = new DfmIssue(
            Icon: "icon", Title: "Overhang", Description: "desc",
            Severity: DfmIssueSeverity.Warning,
            Category: "overhang",
            OverlayKey: "FDM__overhang",
            OverlayUrl: "https://example.com/overhang.glb");

        await InvokeHandleToggleOverlay(cut.Instance, issue);

        var toggleCalls = moduleInterop.Invocations
            .Where(i => i.Identifier == "toggleDfmOverlay")
            .ToList();
        Assert.Single(toggleCalls);
        Assert.Equal("FDM__overhang", toggleCalls[0].Arguments[1]);
    }

    [Fact]
    public async Task HandleToggleOverlay_DoesNotCoToggle_ForNonOverhangCategory()
    {
        var moduleInterop = JSInterop.SetupModule(ViewerModuleUrl.Url);

        var part = MakePartWithOverlayUrls(new Dictionary<string, string>
        {
            ["FDM__thin_wall"]         = "https://example.com/thin_wall.glb",
            // support key does NOT exist — no co-toggle expected for thin_wall
            ["FDM__overhang_support"]  = "https://example.com/overhang_support.glb",
        });

        var cut = Render<PartDetailCard>(p => p
            .Add(x => x.Part, part)
            .Add(x => x.TempProjectId, Guid.NewGuid()));

        var issue = new DfmIssue(
            Icon: "icon", Title: "Thin wall", Description: "desc",
            Severity: DfmIssueSeverity.Warning,
            Category: "thin_wall",
            OverlayKey: "FDM__thin_wall",
            OverlayUrl: "https://example.com/thin_wall.glb");

        await InvokeHandleToggleOverlay(cut.Instance, issue);

        var toggleCalls = moduleInterop.Invocations
            .Where(i => i.Identifier == "toggleDfmOverlay")
            .ToList();
        Assert.Single(toggleCalls);
        Assert.Equal("FDM__thin_wall", toggleCalls[0].Arguments[1]);
    }
}
