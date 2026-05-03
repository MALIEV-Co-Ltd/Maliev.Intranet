using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared.Dtos;
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

    private static CncDfmReportPayload CncTurningReport(
        IReadOnlyList<CncDfmReportPayloadIssuesItem> issues) => new(
        ReportType: "CNC_TURN",
        SharpCornerCount: 0,
        SharpCornerRegions: [],
        HasUndercuts: false,
        UndercutRegions: [],
        HasDrillHoles: false,
        DrillHoleCount: 0,
        RequiresEdm: false,
        RequiresGrinding: false,
        MinimumFeatureSizeMm: 0,
        IsTurnable: issues.Count == 0,
        PrimaryAxis: "Z",
        AxisVector: [0, 0, 1],
        LengthDiameterRatio: 1,
        SymmetryDeviation: issues.Count == 0 ? 0 : 1,
        Issues: issues);

    private static CncDfmReportPayload CncMillingReport(
        IReadOnlyList<CncDfmReportPayloadIssuesItem> issues) => new(
        ReportType: "CNC_MILL",
        SharpCornerCount: 0,
        SharpCornerRegions: [],
        HasUndercuts: false,
        UndercutRegions: [],
        HasDrillHoles: false,
        DrillHoleCount: 0,
        RequiresEdm: false,
        RequiresGrinding: false,
        MinimumFeatureSizeMm: 1,
        IsTurnable: true,
        PrimaryAxis: string.Empty,
        AxisVector: [],
        LengthDiameterRatio: 0,
        SymmetryDeviation: 0,
        Issues: issues);

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

    // Test 6: FILE_MISSING error code — shows message without "retry" prompt
    [Fact]
    public void DfmTab_FileMissing_ShowsMessageWithoutRetryPrompt()
    {
        var part = CleanFdmPart(timedOut: false);
        part.AnalysisErrorCode = "FILE_MISSING";
        part.DfmReport = null;

        var cut = RenderTab(part);

        Assert.Contains("File is no longer available", cut.Markup);
        Assert.DoesNotContain("Select the process again to retry", cut.Markup);
    }

    // Test 7: Non-FILE_MISSING error code — shows "retry" prompt
    [Fact]
    public void DfmTab_NonFileMissingError_ShowsRetryPrompt()
    {
        var part = CleanFdmPart(timedOut: true);
        part.AnalysisErrorCode = "DFM_ANALYZER_FAILED";

        var cut = RenderTab(part);

        Assert.Contains("Select the process again to retry", cut.Markup);
    }

    // ── Pending state tests (new per-check state machine) ─────────────

    // Test 8: When IsManifold=null and BodyCount=null, both intrinsic checks render as skeletons.
    [Fact]
    public void DfmChecksTab_IntrinsicChecksPendingWhenFlagsAreNull()
    {
        var part = new PartViewModel
        {
            ProcessCode = "FDM",
            IsManifold = null,
            BodyCount = null,
            DfmReport = null,
            DfmAnalysisTimedOut = false,
        };
        var cut = RenderTab(part);

        // Intrinsic checks show as skeletons (not expansion panels) when data is null.
        Assert.Contains("dfm-check-row--skeleton", cut.Markup);
        // Neither intrinsic check should show as a resolved panel (no green check icon).
        Assert.DoesNotContain("dfm-expansion-panel--pass", cut.Markup);
    }

    // Test 9: When IsManifold=true and BodyCount=1, both intrinsic checks show as passed panels.
    [Fact]
    public void DfmChecksTab_IntrinsicChecksPassedWhenDataPresent()
    {
        var part = new PartViewModel
        {
            ProcessCode = "FDM",
            IsManifold = true,
            BodyCount = 1,
            DfmReport = null, // Process checks still pending
            DfmAnalysisTimedOut = false,
        };
        var cut = RenderTab(part);

        // Both intrinsic checks must be resolved as passed expansion panels.
        Assert.Contains("dfm-expansion-panel--pass", cut.Markup);
        // Process checks remain as skeletons.
        Assert.Contains("dfm-check-row--skeleton", cut.Markup);
        // Badge must NOT be ok (process checks still pending).
        Assert.DoesNotContain("dfm-badge--ok", cut.Markup);
    }

    // Test 10: When IsManifold=true, BodyCount=1 with a clean FDM report — all checks pass, badge is ok.
    [Fact]
    public void DfmChecksTab_AllPassedBadgeOkWhenNoPendingAndNoFailed()
    {
        var part = new PartViewModel
        {
            ProcessCode = "FDM",
            IsManifold = true,
            BodyCount = 1,
            DfmAnalysisTimedOut = false,
        };
        var report = AllPassFdmReport();
        part.FdmDfmReport = report;
        part.ResolveDfmReport();

        var cut = RenderTab(part);

        Assert.Contains("dfm-badge--ok", cut.Markup);
        Assert.DoesNotContain("dfm-check-row--skeleton", cut.Markup);
    }

    // Test 11: Multi-body file (BodyCount=14) must fail the "Single body" check.
    [Fact]
    public void DfmChecksTab_MultiBodyFailsSingleBodyCheck()
    {
        var part = new PartViewModel
        {
            ProcessCode = "FDM",
            IsManifold = true,
            BodyCount = 14,
            DfmAnalysisTimedOut = false,
        };
        var report = AllPassFdmReport();
        part.FdmDfmReport = report;
        part.ResolveDfmReport();

        var cut = RenderTab(part);

        // Single body check must be failed.
        Assert.Contains("dfm-expansion-panel--fail", cut.Markup);
        Assert.Contains("14 bodies found", cut.Markup);
        // Badge must not be ok.
        Assert.DoesNotContain("dfm-badge--ok", cut.Markup);
    }

    // Test 12: Pass-state detail for "Single body" uses the measured body count, not static text.
    [Fact]
    public void DfmChecksTab_PassDetailForSingleBodyUsesMeasuredCount()
    {
        var part = new PartViewModel
        {
            ProcessCode = "FDM",
            IsManifold = true,
            BodyCount = 1,
            DfmAnalysisTimedOut = false,
        };
        var report = AllPassFdmReport();
        part.FdmDfmReport = report;
        part.ResolveDfmReport();

        var cut = RenderTab(part);

        // Pass detail must contain the measured value "1 solid body", not just static text.
        Assert.Contains("1 solid body", cut.Markup);
        // Should NOT contain vague canned text that ignores data.
        Assert.DoesNotContain("File contains a single solid body", cut.Markup);
    }

    // Test 13: "Mesh integrity" passes with watertight text; no "Pending" skeleton when IsManifold=true.
    [Fact]
    public void DfmChecksTab_MeshIntegrityPassDetailShowsWatertight()
    {
        var part = new PartViewModel
        {
            ProcessCode = "FDM",
            IsManifold = true,
            BodyCount = 1,
            DfmAnalysisTimedOut = false,
        };
        var report = AllPassFdmReport();
        part.FdmDfmReport = report;
        part.ResolveDfmReport();

        var cut = RenderTab(part);

        Assert.Contains("watertight", cut.Markup);
    }

    // Test 14: When IsManifold=false, "Mesh integrity" shows as failed regardless of DfmReport.
    [Fact]
    public void DfmChecksTab_NonManifoldMeshFailsMeshIntegrityCheck()
    {
        var part = new PartViewModel
        {
            ProcessCode = "FDM",
            IsManifold = false,
            BodyCount = 1,
            DfmAnalysisTimedOut = false,
        };
        var report = AllPassFdmReport();
        part.FdmDfmReport = report;
        part.ResolveDfmReport();

        var cut = RenderTab(part);

        Assert.Contains("dfm-expansion-panel--fail", cut.Markup);
        Assert.Contains("Mesh has geometry issues", cut.Markup);
    }

    // Test 15: DfmReport DTO type (from two-phase HTTP path) with issues → shows failures, not phantom passes.
    [Fact]
    public void DfmChecksTab_DfmReportDtoType_ShowsFailuresNotPhantomPasses()
    {
        var part = new PartViewModel
        {
            ProcessCode = "FDM",
            IsManifold = true,
            BodyCount = 1,
            DfmAnalysisTimedOut = false,
        };
        // Simulate a DfmReport DTO from the two-phase HTTP path (not a typed FdmDfmReportPayload).
        var report = new Maliev.Intranet.Shared.Dtos.DfmReport
        {
            ReportType = "FDM",
            Issues =
            [
                new Maliev.Intranet.Shared.Dtos.DfmIssue { Category = "thin_wall", Severity = "warning", Title = "6 thin wall regions", Description = "Wall thickness below minimum", Value = 6, Threshold = 0.8 },
                new Maliev.Intranet.Shared.Dtos.DfmIssue { Category = "overhang", Severity = "warning", Title = "5 overhang regions", Description = "Overhang exceeds angle limit", Value = 5, Threshold = 45 },
            ],
        };
        part.FdmDfmReport = report;
        part.ResolveDfmReport();

        var cut = RenderTab(part);

        // Must show failure panels, not just passes.
        Assert.Contains("dfm-expansion-panel--fail", cut.Markup);
        // Badge must NOT be ok — there are failures.
        Assert.DoesNotContain("dfm-badge--ok", cut.Markup);
        Assert.DoesNotContain("dfm-check-row--skeleton", cut.Markup);
    }

    // Test 16: DfmReport DTO type with no issues → all checks pass, badge is ok.
    [Fact]
    public void DfmChecksTab_DfmReportDtoType_AllPassWhenNoIssues()
    {
        var part = new PartViewModel
        {
            ProcessCode = "FDM",
            IsManifold = true,
            BodyCount = 1,
            DfmAnalysisTimedOut = false,
        };
        var report = new Maliev.Intranet.Shared.Dtos.DfmReport
        {
            ReportType = "FDM",
            Issues = [],
        };
        part.FdmDfmReport = report;
        part.ResolveDfmReport();

        var cut = RenderTab(part);

        Assert.Contains("dfm-badge--ok", cut.Markup);
        Assert.DoesNotContain("dfm-expansion-panel--fail", cut.Markup);
        Assert.DoesNotContain("dfm-check-row--skeleton", cut.Markup);
    }

    [Fact]
    public void DfmChecksTab_CncTurningNotTurnableIssue_ShowsFailureInTab()
    {
        var part = new PartViewModel
        {
            ProcessCode = "CNC_TURN",
            IsManifold = true,
            BodyCount = 1,
            DfmAnalysisTimedOut = false,
        };
        part.CncDfmReport = CncTurningReport(
        [
            new CncDfmReportPayloadIssuesItem(
                Category: "not_turnable",
                Severity: "error",
                Title: "Part Not Suitable for Turning",
                Description: "Symmetry deviation 1.000 exceeds turning threshold. Part likely requires milling.",
                Value: 1.0,
                Threshold: 0.15),
        ]);
        part.ResolveDfmReport();

        var cut = RenderTab(part);

        Assert.Contains("Turning suitability", cut.Markup);
        Assert.Contains("Part Not Suitable for Turning", cut.Markup);
        Assert.Contains("Symmetry deviation 1.000 exceeds turning threshold", cut.Markup);
        Assert.Contains("dfm-expansion-panel--fail", cut.Markup);
        Assert.Contains("4/5 checks passed", cut.Markup);
        Assert.DoesNotContain("7/7 checks passed", cut.Markup);
        Assert.DoesNotContain("dfm-badge--ok", cut.Markup);
    }

    [Fact]
    public void PartViewModel_CncTypedIssues_MarkPartAsHavingDfmIssues()
    {
        var part = new PartViewModel
        {
            ProcessCode = "CNC_TURN",
            IsManifold = true,
            BodyCount = 1,
        };
        part.CncDfmReport = CncTurningReport(
        [
            new CncDfmReportPayloadIssuesItem(
                Category: "not_turnable",
                Severity: "error",
                Title: "Part Not Suitable for Turning",
                Description: "Symmetry deviation 1.000 exceeds turning threshold. Part likely requires milling.",
                Value: 1.0,
                Threshold: 0.15),
        ]);
        part.ResolveDfmReport();

        Assert.True(part.HasProcessRelevantDfmIssues);
    }

    [Fact]
    public void DfmChecksTab_CncMillingCavityAndSharpCornerIssues_ShowFailuresInTab()
    {
        var part = new PartViewModel
        {
            ProcessCode = "CNC_MILL",
            IsManifold = true,
            BodyCount = 1,
            DfmAnalysisTimedOut = false,
        };
        part.CncDfmReport = CncMillingReport(
        [
            new CncDfmReportPayloadIssuesItem(
                Category: "cavity_depth",
                Severity: "warning",
                Title: "Deep Cavities (13)",
                Description: "13 cavity/cavities exceed the 4.0:1 depth/width limit. Worst: 15.4:1.",
                Value: 13,
                Threshold: 4.0),
            new CncDfmReportPayloadIssuesItem(
                Category: "sharp_corner",
                Severity: "warning",
                Title: "Sharp Internal Corners (29)",
                Description: "29 sharp corner(s) may require EDM or are inaccessible to standard endmills.",
                Value: 29,
                Threshold: 1.0),
        ]);
        part.ResolveDfmReport();

        var cut = RenderTab(part);

        Assert.Contains("Deep cavities", cut.Markup);
        Assert.Contains("Deep Cavities (13)", cut.Markup);
        Assert.Contains("Sharp Internal Corners (29)", cut.Markup);
        Assert.Contains("dfm-expansion-panel--fail", cut.Markup);
        Assert.Contains("6/8 checks passed", cut.Markup);
        Assert.DoesNotContain("7/7 checks passed", cut.Markup);
        Assert.DoesNotContain("dfm-badge--ok", cut.Markup);
    }
}
