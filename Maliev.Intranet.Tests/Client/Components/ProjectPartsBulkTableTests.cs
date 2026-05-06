using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class ProjectPartsBulkTableTests : BunitContext, IAsyncLifetime
{
    public ProjectPartsBulkTableTests()
    {
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddSingleton(new HttpClient { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton<CurrencyService>();
        Services.AddSingleton(new UploadSettings());
        Services.AddSingleton(CreateFileTypesSettings());
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ProjectPartsBulkTable_WithParts_RendersDesktopTableAndMobileCards()
    {
        var parts = CreateParts();

        var cut = RenderTable(parts);

        Assert.NotEmpty(cut.FindAll(".pbt-table-row"));
        Assert.NotEmpty(cut.FindAll(".pbt-part-card"));
        Assert.NotEmpty(cut.FindAll(".pbt-part-thumb-img"));
        Assert.Contains("Table edit", cut.Markup);
    }

    [Fact]
    public void ProjectPartsBulkTable_WhenOnePartSelected_ShowsSelectionCountAndBulkToolbar()
    {
        var parts = CreateParts();
        var selected = new HashSet<PartViewModel>(ReferenceEqualityComparer.Instance) { parts[0] };

        var cut = RenderTable(parts, selected);

        Assert.Contains("1 selected", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".pbt-bulk-panel"));
        Assert.DoesNotContain("Apply to selected", cut.Markup);
    }

    [Fact]
    public void ProjectPartsBulkTable_WhenBulkProcessSelected_AppliesPatchImmediately()
    {
        var parts = CreateParts();
        var selected = new HashSet<PartViewModel>(ReferenceEqualityComparer.Instance) { parts[0], parts[1] };
        ProjectPartsBulkApplyRequest? request = null;

        var cut = RenderTable(
            parts,
            selected,
            bulkApply: EventCallback.Factory.Create<ProjectPartsBulkApplyRequest>(
                this,
                value => request = value));

        cut.Find(".pbt-bulk-grid select").Change("FDM");

        Assert.NotNull(request);
        Assert.Equal(2, request.Parts.Count);
        Assert.True(request.Patch.IncludeProcess);
        Assert.Equal("FDM", request.Patch.Process?.Code);
        Assert.False(request.ShowSummary);
    }

    [Fact]
    public void ProjectPartsBulkTable_WhenDfmReviewBadgeClicked_RaisesReviewAction()
    {
        var parts = CreateParts();
        parts[0].ProcessCode = "FDM";
        parts[0].FdmDfmReport = new DfmReport
        {
            ReportType = "FDM",
            Issues =
            [
                new Maliev.Intranet.Shared.Dtos.DfmIssue
                {
                    Category = "thin_wall",
                    Severity = "warning",
                    Title = "Thin wall",
                },
            ],
        };
        parts[0].ResolveDfmReport();
        ProjectPartDfmActionRequest? request = null;

        var cut = RenderTable(
            parts,
            dfmAction: EventCallback.Factory.Create<ProjectPartDfmActionRequest>(
                this,
                value => request = value));

        cut.Find(".pbt-dfm--warning").Click();

        Assert.NotNull(request);
        Assert.Same(parts[0], request.Part);
        Assert.Equal(ProjectPartDfmAction.Review, request.Action);
    }

    [Fact]
    public void ProjectPartsBulkTable_WhenThumbnailClicked_ShowsLargePreview()
    {
        var parts = CreateParts();

        var cut = RenderTable(parts);

        cut.Find(".pbt-part-thumb-button").Click();

        var previewImage = cut.Find(".pbt-preview-image");
        Assert.Equal("/thumb-bracket-large.png", previewImage.GetAttribute("src"));
        Assert.Contains("Preview", cut.Markup);
    }

    [Fact]
    public void ProjectPartsBulkTable_WhenLargeThumbnailMissing_PreviewsSmallThumbnail()
    {
        var parts = CreateParts();
        parts[0].ThumbnailLargeUrl = null;

        var cut = RenderTable(parts);

        cut.Find(".pbt-part-thumb-button").Click();

        var previewImage = cut.Find(".pbt-preview-image");
        Assert.Equal("/thumb-bracket.png", previewImage.GetAttribute("src"));
    }

    [Fact]
    public void ProjectPartsBulkTable_WhenUnavailableBadgeClicked_RaisesRetryAction()
    {
        var parts = CreateParts();
        parts[0].DfmAnalysisTimedOut = true;
        ProjectPartDfmActionRequest? request = null;

        var cut = RenderTable(
            parts,
            dfmAction: EventCallback.Factory.Create<ProjectPartDfmActionRequest>(
                this,
                value => request = value));

        cut.Find(".pbt-dfm--error").Click();

        Assert.NotNull(request);
        Assert.Same(parts[0], request.Part);
        Assert.Equal(ProjectPartDfmAction.Retry, request.Action);
    }

    [Fact]
    public void ProjectPartsBulkTable_WhenDuplicateFileIdsExist_SelectsByPartReference()
    {
        var sharedFileId = Guid.NewGuid();
        var first = new PartViewModel { FileId = sharedFileId, Name = "first-copy.stl", Quantity = 1 };
        var second = new PartViewModel { FileId = sharedFileId, Name = "second-copy.stl", Quantity = 1 };
        var parts = new List<PartViewModel> { first, second };
        var selected = new HashSet<PartViewModel>(ReferenceEqualityComparer.Instance) { second };

        var cut = RenderTable(parts, selected);

        Assert.Single(cut.FindAll(".pbt-table-row--selected"));
        Assert.Single(cut.FindAll(".pbt-part-card--selected"));
    }

    [Fact]
    public void ProjectPartsBulkTable_WhenRowExpanded_RendersAdvancedConfigurationPanel()
    {
        var parts = CreateParts();
        var cut = RenderTable(parts);

        cut.Find(".pbt-expand-button").Click();

        Assert.NotEmpty(cut.FindAll(".pbt-advanced-panel"));
    }

    [Fact]
    public void ProjectPartsBulkTable_WhenConfiguratorButtonClicked_RaisesLayoutModeChanged()
    {
        var parts = CreateParts();
        LayoutMode? requestedMode = null;

        var cut = Render<ProjectPartsBulkTable>(parameters => parameters
            .Add(p => p.Parts, parts)
            .Add(p => p.SelectedParts, [])
            .Add(p => p.Processes, [new ProcessDto(Guid.NewGuid(), "FDM", "FDM", null, 10)])
            .Add(p => p.OnLayoutModeChanged, EventCallback.Factory.Create<LayoutMode>(this, mode => requestedMode = mode)));

        cut.Find(".pbt-configurator-button").Click();

        Assert.Equal(LayoutMode.Configurator, requestedMode);
    }

    [Fact]
    public void ProjectPartsBulkTable_WhenUploadClicked_RaisesAddPart()
    {
        var parts = CreateParts();
        var addRequested = false;

        var cut = Render<ProjectPartsBulkTable>(parameters => parameters
            .Add(p => p.Parts, parts)
            .Add(p => p.SelectedParts, [])
            .Add(p => p.Processes, [new ProcessDto(Guid.NewGuid(), "FDM", "FDM", null, 10)])
            .Add(p => p.OnAddPart, EventCallback.Factory.Create(this, () => addRequested = true)));

        cut.Find(".pbt-upload-zone").Click();

        Assert.True(addRequested);
    }

    private RenderedComponent<ProjectPartsBulkTable> RenderTable(
        List<PartViewModel> parts,
        IReadOnlyCollection<PartViewModel>? selectedParts = null,
        EventCallback<ProjectPartsBulkApplyRequest> bulkApply = default,
        EventCallback<ProjectPartDfmActionRequest> dfmAction = default)
    {
        return Render<ProjectPartsBulkTable>(parameters => parameters
            .Add(p => p.Parts, parts)
            .Add(p => p.SelectedParts, selectedParts ?? [])
            .Add(p => p.Processes, [new ProcessDto(Guid.NewGuid(), "FDM", "FDM", null, 10)])
            .Add(p => p.OnBulkApply, bulkApply)
            .Add(p => p.OnDfmAction, dfmAction));
    }

    private static List<PartViewModel> CreateParts()
    {
        var material = new CatalogMaterialDto(Guid.NewGuid(), "PLA", "PLA", "Plastic", null, null, 10);
        var finish = new CatalogSurfaceFinishDto(Guid.NewGuid(), "As printed", "AS_PRINTED", null, 0m, null, 10);
        var tolerance = new CatalogToleranceDto(Guid.NewGuid(), "Standard", "STD", "ISO 2768", "m", null, 0m, 10);
        return
        [
            new()
            {
                FileId = Guid.NewGuid(),
                Name = "bracket.stl",
                ThumbnailSmallUrl = "/thumb-bracket.png",
                ThumbnailLargeUrl = "/thumb-bracket-large.png",
                Quantity = 2,
                MaterialId = material.Id,
                MaterialCode = material.Code,
                FinishId = finish.Id,
                FinishCode = finish.Code,
                ToleranceId = tolerance.Id,
                ToleranceCode = tolerance.Code,
                AvailableMaterials = [material],
                AvailableFinishes = [finish],
                AvailableTolerances = [tolerance],
                EstimatedUnitPrice = 100m,
                EstimatedTotalAmount = 200m,
            },
            new()
            {
                FileId = Guid.NewGuid(),
                Name = "cover.stl",
                Quantity = 1,
                AvailableMaterials = [material],
                AvailableFinishes = [finish],
                AvailableTolerances = [tolerance],
            },
        ];
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
