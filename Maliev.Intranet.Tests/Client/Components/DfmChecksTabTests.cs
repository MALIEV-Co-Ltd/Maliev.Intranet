using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.MessagingContracts.Contracts.Geometry;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public class DfmChecksTabTests : BunitContext, IAsyncLifetime
{
    public DfmChecksTabTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    private RenderedComponent<DfmChecksTab> RenderTab(PartViewModel part)
    {
        return Render<DfmChecksTab>(parameters => parameters
            .Add(p => p.Part, part));
    }

    private static PartViewModel CleanFdmPart(bool timedOut = false, string processCode = "FDM") => new()
    {
        ProcessCode = processCode,
        DfmAnalysisTimedOut = timedOut,
        IsManifold = true,
        BodyCount = 1,
        DfmReport = null,
    };

    private static FdmDfmReportPayload AllPassFdmReport() => new(
        ReportType: "FDM",
        ThinWallCount: 0,
        ThinWallRegions: [],
        OverhangFaceCount: 0,
        OverhangAreaCm2: 0,
        OverhangRegions: [],
        SupportRequired: false,
        EstimatedSupportVolumeCm3: null,
        SmallDetailCount: 0,
        Issues: []);

    // Test 1: When DfmAnalysisTimedOut=true → badge has "dfm-badge--error" class and shows "Analysis failed"
    [Fact]
    public void DfmTab_WhenTimedOut_BadgeShowsErrorClassAndFailedText()
    {
        var part = CleanFdmPart(timedOut: true);
        var cut = RenderTab(part);

        Assert.Contains("dfm-badge--error", cut.Markup);
        Assert.Contains("Analysis failed", cut.Markup);
    }

    // Test 2: When DfmAnalysisTimedOut=true and DfmReport=null → no skeleton, MudAlert rendered
    [Fact]
    public void DfmTab_WhenTimedOutAndNoReport_ShowsAlertNotSkeleton()
    {
        var part = CleanFdmPart(timedOut: true);
        part.DfmReport = null;

        var cut = RenderTab(part);

        Assert.Contains("mud-alert", cut.Markup);
        Assert.DoesNotContain("dfm-check-row--skeleton", cut.Markup);
    }

    // Test 3: When DfmAnalysisTimedOut=false and DfmReport=null and ProcessCode set → skeleton renders (not alert)
    [Fact]
    public void DfmTab_WhenLoadingAndProcessSet_ShowsSkeletonNotAlert()
    {
        var part = CleanFdmPart(timedOut: false);
        part.DfmReport = null;

        var cut = RenderTab(part);

        Assert.Contains("dfm-check-row--skeleton", cut.Markup);
        Assert.DoesNotContain("mud-alert", cut.Markup);
    }

    // Test 4: AllPassed is false when DfmAnalysisTimedOut=true (badge must NOT have dfm-badge--ok)
    [Fact]
    public void DfmTab_WhenTimedOut_BadgeIsNotOkEvenWithCleanMesh()
    {
        var part = CleanFdmPart(timedOut: true);
        part.DfmReport = AllPassFdmReport();

        var cut = RenderTab(part);

        Assert.DoesNotContain("dfm-badge--ok", cut.Markup);
    }

    // Test 5: AllPassed is false when DfmReport is null (loading state — badge must NOT have dfm-badge--ok)
    [Fact]
    public void DfmTab_WhenReportIsNull_BadgeIsNotOk()
    {
        var part = CleanFdmPart(timedOut: false);
        part.DfmReport = null;

        var cut = RenderTab(part);

        Assert.DoesNotContain("dfm-badge--ok", cut.Markup);
    }
}
