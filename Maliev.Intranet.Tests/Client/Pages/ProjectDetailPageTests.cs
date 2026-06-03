using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.Intranet.Tests.Client.Pages;

public sealed class ProjectDetailPageTests : BunitContext, IAsyncLifetime
{
    private readonly Guid _projectId = Guid.Parse("5daabfe7-7b4a-43fe-9287-b596eb75ece8");
    private readonly Guid _quotationId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private readonly Guid _bracketPartId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _sensorPartId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Guid _fixturePartId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private readonly Guid _planningHoldId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _machineId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _jobId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private readonly List<string> _requestedPaths = [];
    private readonly List<string> _requestedRequests = [];
    private JsonDocument? _quotationPdfRequest;
    private JsonDocument? _planningHoldRequest;
    private bool _notePosted;
    private bool _bracketDfmAcknowledged;

    public ProjectDetailPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<LayoutService>(new LayoutService(JSInterop.JSRuntime, NullLogger<LayoutService>.Instance));

        var handler = new MockHttpMessageHandler(HandleRequestAsync);
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://test/") });
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ProjectDetail_RendersEmployeeQuoteRecordLayout()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("PRJ-2026-0184", cut.Markup));

        Assert.Contains("project-record-shell", cut.Markup);
        Assert.Contains("project-record-tabs", cut.Markup);
        Assert.Contains("Overview", cut.Markup);
        Assert.Contains("Quote", cut.Markup);
        Assert.Contains("Parts (3)", cut.Markup);
        Assert.Contains("Planning", cut.Markup);
        Assert.Contains("Timeline", cut.Markup);
        Assert.Contains("Customer", cut.Markup);
        Assert.Contains("Axion Robotics", cut.Markup);
        Assert.Contains("Axion Robotics Co., Ltd.", cut.Markup);
        Assert.Contains("QT-2026-0098", cut.Markup);
        Assert.Contains("Shipping", cut.Markup);
        Assert.Contains("Customer ID 11111111", cut.Markup);
        Assert.Contains("2200 Industrial Pkwy", cut.Markup);
        Assert.Contains("14 Finance Tower", cut.Markup);
        Assert.Contains("Manufacturing summary", cut.Markup);
        Assert.Contains("bracket-left.stl", cut.Markup);
        Assert.Contains("18,250.00", cut.Markup);
        Assert.Contains("aria-label=\"Edit project\"", cut.Markup);
        Assert.Contains("aria-label=\"Regenerate PDF\"", cut.Markup);
        Assert.Contains("project-header-icon-action", cut.Markup);
        Assert.Contains("Accept quote", cut.Markup);

        var expectedLocalUpdated = new DateTime(2026, 4, 18, 14, 22, 0, DateTimeKind.Utc)
            .ToLocalTime()
            .ToString("MMM d, yyyy HH:mm");
        var headerMeta = cut.Find(".mlv-page-meta").TextContent;
        Assert.Contains("Robot arm calibration fixture", headerMeta);
        Assert.Contains("Axion Robotics", headerMeta);
        Assert.Contains($"Last updated on {expectedLocalUpdated}", headerMeta);
        Assert.Contains("Quotation Generated", headerMeta);
        Assert.DoesNotContain("Created", headerMeta);
        Assert.DoesNotContain("Updated", headerMeta);

        var overview = cut.Find(".project-record-grid");
        Assert.Contains("project-record-main-manufacturing", overview.InnerHtml);
        Assert.Contains("project-overview-sidebar", overview.InnerHtml);
        var quoteTerms = cut.Find(".project-information-card").TextContent;
        Assert.Contains("Quote terms", quoteTerms);
        Assert.Contains("Lead time", quoteTerms);
        Assert.Contains("7-10 business days", quoteTerms);
        Assert.DoesNotContain("Project #", quoteTerms);
        Assert.DoesNotContain("Title", quoteTerms);
        Assert.DoesNotContain("Project status", quoteTerms);
        Assert.DoesNotContain("QT-2026-0098", quoteTerms);
        Assert.DoesNotContain("Shipping", quoteTerms);
        Assert.Contains("project-quote-terms", cut.Markup);
        Assert.Contains("project-customer-heading", cut.Markup);
        Assert.Contains("project-customer-detail-grid", cut.Markup);
        Assert.Contains("project-customer-address-block", cut.Markup);
        Assert.Contains("Tax ID 0105559999999", cut.Markup);
        Assert.Contains("Head Office", cut.Markup);
    }

    [Fact]
    public void ProjectDetail_RendersCustomerProfileImageInCustomerCard()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("project-customer-avatar-image", cut.Markup));

        var avatar = cut.Find(".project-customer-avatar-image");
        Assert.Equal("https://lh3.googleusercontent.com/a/axion", avatar.GetAttribute("src"));
        Assert.Equal("no-referrer", avatar.GetAttribute("referrerpolicy"));
        Assert.Equal("true", avatar.GetAttribute("aria-hidden"));
        Assert.Contains("project-customer-identity", cut.Markup);
    }

    [Fact]
    public void ProjectDetail_EditProject_NavigatesBackToProjectNewResumeRoute()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("project-action-edit", cut.Markup));
        cut.Find("button.project-action-edit").Click();

        Assert.EndsWith($"/sales/projects/new?resume={_projectId}", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectDetail_QuoteActions_ReusesExistingPdfAndAcceptQuote()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("project-action-download", cut.Markup));
        cut.Find("button[data-tab='quote']").Click();
        cut.WaitForAssertion(() => Assert.Contains("https://storage.example/quote-v2.pdf", cut.Markup));

        cut.Find("button.project-action-download").Click();
        cut.Find("button.project-action-accept").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(_requestedPaths, path => path == "/api/v1/quotations/draft-pdf");
            Assert.DoesNotContain(_requestedPaths, path => path == $"/api/v1/quotations/{_quotationId}/pdf/latest");
            Assert.DoesNotContain(_requestedPaths, path => path == $"/api/v1/quotations/{_quotationId}/pdf");
            Assert.DoesNotContain(_requestedPaths, path => path == $"/api/v1/projects/{_projectId}/accept-quotation");
            Assert.Null(_quotationPdfRequest);
            Assert.Contains(
                JSInterop.Invocations,
                invocation => invocation.Identifier == "window.open"
                    && invocation.Arguments.Count == 2
                    && string.Equals(invocation.Arguments[0]?.ToString(), "https://storage.example/quote-v2.pdf", StringComparison.Ordinal)
                    && string.Equals(invocation.Arguments[1]?.ToString(), "_blank", StringComparison.Ordinal));
        });

        cut.WaitForAssertion(() => Assert.Contains("https://storage.example/quote-v2.pdf", cut.Markup));

        cut.Find("button.project-accept-confirm").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedPaths, path => path == $"/api/v1/projects/{_projectId}/accept-quotation");
            Assert.Contains("Accepted", cut.Markup);
        });
    }

    [Fact]
    public void ProjectDetail_QuoteTab_RendersDocumentAndCommercialBreakdown()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Quote", cut.Markup));
        cut.Find("button[data-tab='quote']").Click();

        Assert.Contains("project-quote-workspace", cut.Markup);
        Assert.Contains("Quote document", cut.Markup);
        Assert.Contains("Quote revision history", cut.Markup);
        Assert.Contains("Commercial breakdown", cut.Markup);
        Assert.Contains("https://storage.example/quote-v2.pdf", cut.Markup);
        Assert.Contains("Version 2", cut.Markup);
        Assert.Contains("Current", cut.Markup);
        Assert.Contains("Updated finish and delivery terms.", cut.Markup);
        Assert.DoesNotContain("No PDF generated", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Line subtotal", cut.Markup);
        Assert.Contains("Parts subtotal", cut.Markup);
        Assert.Contains("Shipping", cut.Markup);
        Assert.Contains("Not quoted separately", cut.Markup);
        Assert.Contains("Tax / VAT", cut.Markup);
        Assert.Contains("bracket-left.stl", cut.Markup);
        Assert.Contains("sensor-cover.3mf", cut.Markup);
        Assert.Contains("18,250.00", cut.Markup);
    }

    [Fact]
    public void ProjectDetail_QuoteTab_RendersPdfObjectWithFallbackInsteadOfIframe()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Quote", cut.Markup));
        cut.Find("button[data-tab='quote']").Click();

        Assert.Contains("<object", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("type=\"application/pdf\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("data=\"https://storage.example/quote-v2.pdf\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("PDF preview could not be loaded", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<iframe", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectDetail_PlanningTab_RendersMultiMachineScheduleBoard()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Planning", cut.Markup));
        cut.Find("button[data-tab='planning']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Production planning", cut.Markup));

        Assert.Contains(_requestedPaths, path => path == $"/api/v1/projects/{_projectId}/production-plan");
        Assert.Contains("project-planning-panel", cut.Markup);
        Assert.Contains("project-planning-workspace", cut.Markup);
        Assert.Contains("project-planning-routing", cut.Markup);
        Assert.Contains("project-planning-queue", cut.Markup);
        Assert.Contains("project-planning-selected", cut.Markup);
        Assert.Contains("project-planning-table", cut.Markup);
        Assert.Contains("project-planning-part", cut.Markup);
        Assert.Contains($"data-project-part-id=\"{_bracketPartId}\"", cut.Markup);
        Assert.Contains($"data-selected-part-id=\"{_bracketPartId}\"", cut.Markup);
        Assert.Contains("https://storage.example/bracket-thumb.webp", cut.Markup);
        Assert.Contains("3 quoted parts", cut.Markup);
        Assert.Contains("Manufacturing", cut.Markup);
        Assert.Contains("Qty / DFM", cut.Markup);
        Assert.DoesNotContain("<th>Actions</th>", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("bracket-left.stl", cut.Markup);
        Assert.Contains("120 x 64 x 18 mm", cut.Markup);
        Assert.Contains("CNC Milling", cut.Markup);
        Assert.Contains("Aluminium 6061-T6", cut.Markup);
        Assert.Contains("Anodized - Black - ISO 2768-m", cut.Markup);
        Assert.DoesNotContain("AS_MACHINED", cut.Markup);
        Assert.Contains("Requires acknowledgement", cut.Markup);
        Assert.Contains("CNC Mill 01", cut.Markup);
        Assert.Contains("2 queued ahead", cut.Markup);
        Assert.Contains("Hold #3", cut.Markup);
        Assert.Contains("CCCCCCCC", cut.Markup);
        Assert.Contains("View machine queue", cut.Markup);
        Assert.Contains("production-schedule-board", cut.Markup);
        Assert.Contains("Machine schedule", cut.Markup);
        Assert.Contains("data-machine-id=\"CNC-01\"", cut.Markup);
        Assert.Contains("data-machine-id=\"FDM-01\"", cut.Markup);
        Assert.Contains("CNC Mill 01", cut.Markup);
        Assert.Contains("FDM Printer 01", cut.Markup);
        Assert.Contains("psb-slot-job", cut.Markup);
        Assert.Contains("psb-slot-hold", cut.Markup);
        Assert.Contains("psb-slot-proposed", cut.Markup);
        Assert.Contains("psb-slot-current-project", cut.Markup);
        Assert.Contains("--psb-slot-gap: 6px", cut.Markup);
        Assert.Contains("JOB-1001", cut.Markup);
        Assert.Contains("HOLD-AAAA", cut.Markup);
        Assert.Contains("bracket-left.stl", cut.Markup);
        Assert.Contains("sensor-cover.3mf", cut.Markup);
        Assert.DoesNotContain("project-planning-queue-panel", cut.Markup);
        Assert.Contains("Create 72-hour planning hold", cut.Markup);

        cut.Find($"tr[data-project-part-id='{_sensorPartId}']").Click();

        Assert.Contains("Update planning hold", cut.Markup);
        Assert.Contains("Cancel planning hold", cut.Markup);
    }

    [Fact]
    public async Task ProjectDetail_PlanningQueueButton_FocusesMachineRowAndSlotInBoard()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Planning", cut.Markup));
        cut.Find("button[data-tab='planning']").Click();
        cut.WaitForAssertion(() => Assert.Contains("production-schedule-board", cut.Markup));

        Assert.Contains("CNC Mill 01", cut.Markup);
        Assert.Contains("data-focused-machine-id=\"CNC-01\"", cut.Markup);

        await cut.InvokeAsync(() => cut.Find($"tr[data-project-part-id='{_sensorPartId}']").Click());
        await cut.InvokeAsync(() => cut.Find("button[aria-label='View selected machine queue']").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains($"data-selected-part-id=\"{_sensorPartId}\"", cut.Markup);
            Assert.Contains("data-focused-machine-id=\"FDM-01\"", cut.Markup);
            Assert.Contains("class=\"psb-machine-row focused\"", cut.Markup);
            Assert.Contains($"data-project-part-id=\"{_sensorPartId}\"", cut.Markup);
            Assert.Contains(
                JSInterop.Invocations,
                invocation => invocation.Identifier == "malievProductionSchedule.scrollFocusedMachineIntoView");
        });
    }

    [Fact]
    public async Task ProjectDetail_PlanningDayView_DropsProposedSlotIntoHalfHourPlanningHold()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Planning", cut.Markup));
        cut.Find("button[data-tab='planning']").Click();
        cut.WaitForAssertion(() => Assert.Contains("production-schedule-board", cut.Markup));

        cut.FindAll(".psb-zoom button").First(button => button.TextContent == "Day").Click();
        cut.Find("button[aria-label='Next schedule range']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-time-slot=\"2026-04-19T00:00:00.0000000Z\"", cut.Markup);
            Assert.Contains("data-time-slot=\"2026-04-19T23:30:00.0000000Z\"", cut.Markup);
            Assert.Contains("00:00", cut.Markup);
            Assert.Contains("23:30", cut.Markup);
        });

        var proposed = cut.Find("button.psb-slot-proposed");
        Assert.Equal("true", proposed.GetAttribute("draggable"));

        await proposed.TriggerEventAsync("ondragstart", new DragEventArgs());
        await cut.Find("button.psb-slot-drop-target[data-machine-id='CNC-01'][data-time-slot='2026-04-19T13:00:00.0000000Z']")
            .TriggerEventAsync("ondrop", new DragEventArgs());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedRequests, request => request == $"POST /api/v1/projects/{_projectId}/parts/{_bracketPartId}/planning-hold");
            Assert.NotNull(_planningHoldRequest);
        });

        var root = _planningHoldRequest!.RootElement;
        Assert.Equal("CNC-01", root.GetProperty("machineId").GetString());
        Assert.Equal("2026-04-19T13:00:00Z", root.GetProperty("scheduledStartTime").GetDateTime().ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
        Assert.Equal("2026-04-19T16:00:00Z", root.GetProperty("scheduledEndTime").GetDateTime().ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    [Fact]
    public void ProjectDetail_PlanningActions_CallPlanningEndpoints()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Planning", cut.Markup));
        cut.Find("button[data-tab='planning']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Production planning", cut.Markup));

        cut.Find("button.project-planning-create-hold").Click();
        cut.WaitForAssertion(() =>
            Assert.Contains(_requestedRequests, request => request == $"POST /api/v1/projects/{_projectId}/parts/{_bracketPartId}/planning-hold"));

        cut.Find($"tr[data-project-part-id='{_sensorPartId}']").Click();

        cut.Find("button.project-planning-update-hold").Click();
        cut.WaitForAssertion(() =>
            Assert.Contains(_requestedRequests, request => request == $"PATCH /api/v1/projects/{_projectId}/planning-holds/{_planningHoldId}"));

        cut.Find("button.project-planning-cancel-hold").Click();
        cut.WaitForAssertion(() =>
            Assert.Contains(_requestedRequests, request => request == $"DELETE /api/v1/projects/{_projectId}/planning-holds/{_planningHoldId}"));
    }

    [Fact]
    public void ProjectDetail_WhenTabQueryRequestsParts_RendersPartsTab()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/sales/projects/{_projectId}?tab=parts");

        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("project-parts-panel", cut.Markup));

        Assert.Contains("project-record-body--parts", cut.Markup);
        Assert.Contains("bracket-left.stl", cut.Markup);
        Assert.Contains("sensor-cover.3mf", cut.Markup);
        Assert.DoesNotContain("project-record-grid", cut.Markup);
    }

    [Fact]
    public void ProjectDetail_RendersHumanReadableManufacturingLanguage()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Quotation Generated", cut.Markup));

        Assert.Contains("CNC Milling", cut.Markup);
        Assert.Contains("3D Printing (FDM)", cut.Markup);
        Assert.Contains("As printed", cut.Markup);
        Assert.Contains("As Machined", cut.Markup);
        Assert.Contains("Standard FDM settings", cut.Markup);
        Assert.Contains("ISO 2768-m", cut.Markup);
        Assert.Contains("project-material-stack", cut.Markup);
        Assert.Contains("project-material-color", cut.Markup);
        Assert.Contains("project-material-swatch", cut.Markup);
        Assert.Contains("#111827", cut.Markup);
        Assert.Contains("project-config-primary", cut.Markup);
        Assert.Contains("project-config-subtitle", cut.Markup);
        Assert.DoesNotContain("AS_PRINTED", cut.Markup);
        Assert.DoesNotContain("FDM_STD", cut.Markup);
        Assert.DoesNotContain("Iso2768 M", cut.Markup);
    }

    [Fact]
    public void ProjectDetail_RendersThumbnailsAttachmentsAndDfmGate()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("project-part-thumb", cut.Markup));

        Assert.Contains("https://storage.example/bracket-thumb.webp", cut.Markup);
        Assert.Contains("bracket-left-drawing.pdf", cut.Markup);
        Assert.Contains("customer-po.pdf", cut.Markup);
        Assert.Contains("DFM warnings", cut.Markup);
        Assert.Contains("Requires acknowledgement", cut.Markup);
        Assert.Contains("project-dfm-copy", cut.Markup);
        Assert.Contains("project-dfm-cell", cut.Markup);
        Assert.Contains("project-dfm-ack-button", cut.Markup);
        Assert.Contains("Acknowledge", cut.Markup);
        Assert.Contains("DFM issue results", cut.Markup);
        Assert.Contains("Detected checks", cut.Markup);
        Assert.Contains("Overhang", cut.Markup);
        Assert.Contains("DFM acknowledged", cut.Markup);
        Assert.Contains("Warnings reviewed", cut.Markup);
        Assert.Contains("DFM passed", cut.Markup);
        Assert.Contains("No reported issues", cut.Markup);
        Assert.DoesNotContain("project-dfm-icon", cut.Markup);
        Assert.DoesNotContain("DFM pending", cut.Markup);
        Assert.DoesNotContain("Awaiting review", cut.Markup);
    }

    [Fact]
    public void ProjectDetail_ThumbnailClick_OpensLargeThumbnailPopout()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("project-part-thumb-button", cut.Markup));

        cut.Find("button.project-part-thumb-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("project-thumbnail-popout", cut.Markup);
            Assert.Contains("bracket-left.stl", cut.Markup);
            Assert.Contains("https://storage.example/bracket-large.webp", cut.Markup);
            Assert.Contains($"/api/v1/projects/{_projectId}/parts/{_bracketPartId}/thumbnail-large-url", _requestedPaths);
        });
    }

    [Fact]
    public void ProjectDetail_ThumbnailPopout3dToggle_ReplacesImageWithModelViewer()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("project-part-thumb-button", cut.Markup));
        cut.Find("button.project-part-thumb-button").Click();

        cut.WaitForAssertion(() => Assert.Contains("project-thumbnail-view-toggle", cut.Markup));
        cut.Find("button.project-thumbnail-view-toggle").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("project-thumbnail-viewer-frame", cut.Markup);
            Assert.Contains("model-viewer-container", cut.Markup);
            Assert.Contains("babylon-canvas-", cut.Markup);
            Assert.DoesNotContain("bracket-large.webp", cut.Markup);
        });
    }

    [Fact]
    public void ProjectDetail_QuickAcknowledgeDfm_UpdatesPartAndRemovesAction()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("project-dfm-ack-button", cut.Markup));
        cut.Find("button.project-dfm-ack-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedRequests, request => request == $"PUT /api/v1/projects/{_projectId}/parts/{_bracketPartId}");
            Assert.Contains("DFM acknowledged", cut.Markup);
            Assert.DoesNotContain("project-dfm-ack-button", cut.Markup);
        });
    }

    [Fact]
    public void ProjectDetail_AddInternalNote_PostsToProjectNotesEndpoint()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Notes (0)", cut.Markup));
        cut.Find("button[data-tab='notes']").Click();
        cut.Find("textarea.project-note-input").Input("Check customer's drawing revision before release.");
        cut.Find("button.project-note-add").Click();

        var expectedLocalTimestamp = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)
            .ToLocalTime()
            .ToString("MMM d, yyyy HH:mm");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedPaths, path => path == $"/api/v1/projects/{_projectId}/notes");
            Assert.Contains("Saved", cut.Markup);
            Assert.Contains("project-notes-grid", cut.Markup);
            Assert.Contains("project-note-list-column", cut.Markup);
            Assert.Contains("project-note-compose-column", cut.Markup);
            Assert.Contains("Created by Alex Kim", cut.Markup);
            Assert.Contains(expectedLocalTimestamp, cut.Markup);
        });

        cut.Find("button[data-tab='timeline']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Internal note added by Alex Kim", cut.Markup));
    }

    [Fact]
    public void ProjectDetailCss_TargetsRenderFragmentContentThroughDeepSelectors()
    {
        var cssPath = FindProjectDetailCssPath();
        var css = File.ReadAllText(cssPath);

        Assert.Contains("::deep .project-record-body", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-record-body--parts", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-header-subtitle", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-header-icon-action", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-field-grid", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-metric-row", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-parts-panel", css, StringComparison.Ordinal);
        Assert.Contains(".project-parts-panel .project-table-wrap", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-parts-table", css, StringComparison.Ordinal);
        var partsTableRuleStart = css.IndexOf("::deep .project-parts-table th,", StringComparison.Ordinal);
        var partsTableRuleEnd = css.IndexOf('}', partsTableRuleStart);
        var partsTableRule = css[partsTableRuleStart..partsTableRuleEnd];
        Assert.Contains("vertical-align: top;", partsTableRule, StringComparison.Ordinal);
        Assert.Contains("::deep .project-material-stack", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-material-swatch", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-config-stack", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-part-thumb-button", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-part-attachments", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-document-preview-object", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-document-preview-fallback", css, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: anywhere", css, StringComparison.Ordinal);
        Assert.Contains("word-break: break-word", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-planning-part > div", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-planning-part strong", css, StringComparison.Ordinal);
        Assert.Contains("align-items: start", css, StringComparison.Ordinal);
        Assert.Contains(".project-thumbnail-popout", css, StringComparison.Ordinal);
        Assert.Contains(".project-thumbnail-view-toggle", css, StringComparison.Ordinal);
        Assert.Contains(".project-thumbnail-viewer-frame", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-dfm-copy", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-dfm-hover", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-dfm-cell", css, StringComparison.Ordinal);
        Assert.Contains("isolation: isolate", css, StringComparison.Ordinal);
        Assert.Contains("cursor: pointer", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-dfm-ack-button", css, StringComparison.Ordinal);
        Assert.Contains(".project-dfm-issue-card", css, StringComparison.Ordinal);
        Assert.Contains("z-index: 100", css, StringComparison.Ordinal);
        Assert.Contains("var(--maliev-panel)", css, StringComparison.Ordinal);
        Assert.Contains("0 18px 44px", css, StringComparison.Ordinal);
        Assert.DoesNotContain("project-dfm-icon", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-notes-grid", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-note-audit", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-overview-sidebar", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-manufacturing-card", css, StringComparison.Ordinal);
        var recordColumnRuleStart = css.IndexOf("::deep .project-record-main,", StringComparison.Ordinal);
        var recordColumnRuleEnd = css.IndexOf('}', recordColumnRuleStart);
        var recordColumnRule = css[recordColumnRuleStart..recordColumnRuleEnd];
        Assert.Contains("min-width: 0", recordColumnRule, StringComparison.Ordinal);
        Assert.Contains("::deep .project-field-grid-compact", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-snapshot-compact", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-quote-terms", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-customer-heading", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-customer-identity", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-customer-avatar", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-customer-avatar-image", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-customer-detail-grid", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-customer-address-block", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-quote-workspace", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-document-icon .mud-icon-root", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-panel-actions .mud-icon-root", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-document-preview", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-commercial-breakdown", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-commercial-totals", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-planning-panel", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-planning-workspace", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-planning-table", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-planning-selected-actions", css, StringComparison.Ordinal);
        Assert.Contains("::deep .production-schedule-board", css, StringComparison.Ordinal);
        Assert.Contains("::deep .production-schedule-board-shell", css, StringComparison.Ordinal);
        Assert.Contains("th:nth-child(6)", css, StringComparison.Ordinal);
        Assert.Contains("text-align: right", css, StringComparison.Ordinal);
        Assert.Contains("width: 5%", css, StringComparison.Ordinal);
        Assert.Contains("width: 100%", css, StringComparison.Ordinal);
        Assert.Contains("position: sticky", css, StringComparison.Ordinal);
        Assert.Contains("height: 52px", css, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectDetailCss_KeepsOverviewSidebarCompact()
    {
        var cssPath = FindProjectDetailCssPath();
        var css = File.ReadAllText(cssPath);

        var gridRuleStart = css.IndexOf("::deep .project-record-grid {", StringComparison.Ordinal);
        Assert.True(gridRuleStart >= 0, "Project detail CSS should define the overview grid.");

        var gridRuleEnd = css.IndexOf('}', gridRuleStart);
        var gridRule = css[gridRuleStart..gridRuleEnd];

        Assert.Contains("grid-template-columns: minmax(0, 1fr) minmax(320px, 420px);", gridRule, StringComparison.Ordinal);
        Assert.DoesNotContain("520px", gridRule, StringComparison.Ordinal);
    }

    private Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request, CancellationToken _)
    {
        var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;
        _requestedPaths.Add(pathAndQuery);
        _requestedRequests.Add($"{request.Method.Method} {pathAndQuery}");

        if (pathAndQuery.Equals($"/api/v1/projects/{_projectId}", StringComparison.Ordinal))
        {
            return Json(new ProjectDetailDto
            {
                Id = _projectId,
                ProjectNumber = "PRJ-2026-0184",
                CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                CustomerName = "Axion Robotics",
                CustomerProfileImageUrl = "https://lh3.googleusercontent.com/a/axion",
                CustomerEmail = "engineering@axion.example",
                CustomerPhone = "+66 2 555 0101",
                CustomerStatus = "Active",
                CustomerSegment = "Manufacturing",
                CustomerTier = "Enterprise",
                CustomerPreferredLanguage = "en",
                CustomerTimezone = "Asia/Bangkok",
                CustomerCompanyId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                CustomerCompanyName = "Axion Robotics Co., Ltd.",
                CustomerCompanyPhone = "+66 2 555 0100",
                CustomerCompanyEmail = "ops@axion.example",
                CustomerTaxId = "0105559999999",
                CustomerBranch = "Head Office",
                ShippingRecipientName = "Manufacturing Dock",
                ShippingRecipientPhone = "+66 2 555 0199",
                ShippingAddressLine = "2200 Industrial Pkwy, Fremont, CA 94538",
                BillingAddressLine = "14 Finance Tower, Bangkok 10110",
                Title = "Robot arm calibration fixture",
                Status = "QuotationGenerated",
                Currency = "THB",
                TotalPrice = 18250m,
                QuotationId = _quotationId,
                QuotationNumber = "QT-2026-0098",
                QuotationStatus = "Generated",
                CreatedByName = "Alex Kim",
                CreatedAt = new DateTime(2026, 4, 18, 8, 30, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 4, 18, 14, 22, 0, DateTimeKind.Utc),
                ValidUntil = new DateTime(2026, 5, 18, 0, 0, 0, DateTimeKind.Utc),
                Parts =
                [
                    new ProjectPartDto
                    {
                        Id = _bracketPartId,
                        FileId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        FileName = "bracket-left.stl",
                        ProcessType = "CNC_MILL",
                        MaterialName = "Aluminium 6061-T6",
                        Finish = "Anodized",
                        Color = "Black",
                        Tolerance = "ISO2768-m",
                        Quantity = 4,
                        ConfirmedPrice = 2500m,
                        Status = "Confirmed",
                        ThumbnailUrl = "https://storage.example/bracket-thumb.webp",
                        ModelPreviewUrl = "https://storage.example/bracket-left.glb",
                        ThumbnailLargeGcsPath = "customers/axion/projects/prj/bracket-left_thumb_1200.webp",
                        HasDfmWarnings = true,
                        DfmAcknowledged = _bracketDfmAcknowledged,
                        OverlayPaths =
                        {
                            ["overhang"] = "customers/axion/projects/prj/bracket-left_overhang_overlay.glb"
                        },
                        DrawingFiles =
                        [
                            new ProjectPartAttachmentDto
                            {
                                FileName = "bracket-left-drawing.pdf",
                                SignedUrl = "https://storage.example/bracket-left-drawing.pdf"
                            }
                        ],
                        SupplementaryFiles =
                        [
                            new ProjectPartAttachmentDto
                            {
                                FileName = "customer-po.pdf",
                                SignedUrl = "https://storage.example/customer-po.pdf"
                            }
                        ],
                        Dimensions = new ModelDimensionsDto { X = 120, Y = 64, Z = 18 }
                    },
                    new ProjectPartDto
                    {
                        Id = _sensorPartId,
                        FileId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        FileName = "sensor-cover.3mf",
                        ProcessType = "FDM",
                        MaterialName = "PA12 Nylon",
                        Finish = "AS_PRINTED",
                        Color = "FDM_STD",
                        Quantity = 15,
                        ConfirmedPrice = 550m,
                        Status = "Confirmed",
                        HasDfmWarnings = false,
                        DfmAcknowledged = false,
                        OverlayPaths =
                        {
                            ["preview"] = "customers/axion/projects/prj/sensor-cover_preview_overlay.glb"
                        },
                    },
                    new ProjectPartDto
                    {
                        Id = _fixturePartId,
                        FileId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                        FileName = "fixture-base.step",
                        ProcessType = "CNC_MILL",
                        MaterialName = "Aluminium 6061-T6",
                        Finish = "AS_MACHINED",
                        Tolerance = "ISO2768-m",
                        Quantity = 2,
                        ConfirmedPrice = 1200m,
                        Status = "Confirmed",
                        HasDfmWarnings = true,
                        DfmAcknowledged = true,
                    }
                ],
                Timeline =
                [
                    new ProjectTimelineEventDto
                    {
                        Label = "Project created",
                        Timestamp = new DateTime(2026, 4, 18, 8, 30, 0, DateTimeKind.Utc),
                        Completed = true
                    },
                    new ProjectTimelineEventDto
                    {
                        Label = "Quotation generated",
                        Timestamp = new DateTime(2026, 4, 18, 14, 22, 0, DateTimeKind.Utc),
                        Completed = true
                    }
                ],
                Notes = _notePosted
                    ? [
                        new ProjectNoteDto
                        {
                            Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                            ProjectId = _projectId,
                            AuthorName = "Alex Kim",
                            AuthorId = "employee:alex.kim",
                            Content = "Check customer's drawing revision before release.",
                            CreatedAt = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)
                        }
                    ]
                    : []
            });
        }

        if (pathAndQuery.Equals($"/api/v1/quotations/{_quotationId}", StringComparison.Ordinal))
        {
            return Json(BuildQuotationDetail());
        }

        if (pathAndQuery.Equals($"/api/v1/quotations/{_quotationId}/pdf/latest", StringComparison.Ordinal))
        {
            return Json(new { storageUrl = "https://storage.example/quote-auto.pdf" });
        }

        if (pathAndQuery.Equals($"/api/v1/projects/{_projectId}/production-plan", StringComparison.Ordinal))
        {
            return Json(BuildProductionPlan());
        }

        if (pathAndQuery.Equals($"/api/v1/projects/{_projectId}/parts/{_bracketPartId}/thumbnail-large-url", StringComparison.Ordinal))
        {
            return Json(new { Url = "https://storage.example/bracket-large.webp" });
        }

        if (request.Method == HttpMethod.Put
            && pathAndQuery.Equals($"/api/v1/projects/{_projectId}/parts/{_bracketPartId}", StringComparison.Ordinal))
        {
            _bracketDfmAcknowledged = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }

        if (request.Method == HttpMethod.Post
            && pathAndQuery.Equals($"/api/v1/projects/{_projectId}/parts/{_bracketPartId}/planning-hold", StringComparison.Ordinal))
        {
            _planningHoldRequest = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            return Json(BuildPlanningHold(_bracketPartId, 4));
        }

        if (request.Method == HttpMethod.Patch
            && pathAndQuery.Equals($"/api/v1/projects/{_projectId}/planning-holds/{_planningHoldId}", StringComparison.Ordinal))
        {
            return Json(BuildPlanningHold(_sensorPartId, 3));
        }

        if (request.Method == HttpMethod.Delete
            && pathAndQuery.Equals($"/api/v1/projects/{_projectId}/planning-holds/{_planningHoldId}", StringComparison.Ordinal))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }

        if (pathAndQuery.Equals("/api/v1/quotations/draft-pdf", StringComparison.Ordinal))
        {
            _quotationPdfRequest = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { storageUrl = "https://storage.example/quote.pdf" })
            });
        }

        if (pathAndQuery.Equals($"/api/v1/projects/{_projectId}/accept-quotation", StringComparison.Ordinal))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }

        if (pathAndQuery.Equals($"/api/v1/projects/{_projectId}/notes", StringComparison.Ordinal))
        {
            _notePosted = true;
            return Json(new ProjectNoteDto
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                ProjectId = _projectId,
                AuthorName = "Alex Kim",
                AuthorId = "employee:alex.kim",
                Content = "Check customer's drawing revision before release.",
                CreatedAt = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private QuotationDetailDto BuildQuotationDetail() => new()
    {
        Id = _quotationId,
        QuotationNumber = "QT-2026-0098",
        CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        CustomerName = "Axion Robotics",
        CurrentVersionNumber = 2,
        Status = "Generated",
        ValidityPeriodStart = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc),
        ValidityPeriodEnd = new DateTime(2026, 5, 18, 0, 0, 0, DateTimeKind.Utc),
        SubTotal = 19150m,
        Tax = 1340.50m,
        Total = 20490.50m,
        CurrencyCode = "THB",
        DeliveryExpectations = "7-10 business days",
        Versions =
        [
            new QuotationVersionDto
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                VersionNumber = 1,
                TotalPrice = 19000m,
                CurrencyCode = "THB",
                DeliveryExpectations = "10 business days",
                CreatedBy = "Alex Kim",
                CreatedAt = new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc)
            },
            new QuotationVersionDto
            {
                Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                VersionNumber = 2,
                LineItems =
                [
                    new QuotationItemDto
                    {
                        Description = "bracket-left.stl",
                        Quantity = 4,
                        UnitPrice = 2500m
                    },
                    new QuotationItemDto
                    {
                        Description = "sensor-cover.3mf",
                        Quantity = 15,
                        UnitPrice = 550m
                    },
                    new QuotationItemDto
                    {
                        Description = "fixture-base.step",
                        Quantity = 2,
                        UnitPrice = 1200m
                    }
                ],
                TotalPrice = 20490.50m,
                DiscountStructure = new SalesDiscountStructureDto
                {
                    DiscountType = SalesDiscountType.FixedAmount,
                    DiscountValue = 1200m,
                    Conditions = "Automatic bulk-order savings"
                },
                ManualDiscountAmount = 800m,
                ShippingCost = 500m,
                TaxAmount = 1340.50m,
                CurrencyCode = "THB",
                DeliveryExpectations = "7-10 business days",
                SpecialTerms = "50% deposit required before production.",
                ChangeSummary = "Updated finish and delivery terms.",
                ProjectSnapshotHash = "0123456789abcdef",
                PdfArtifactUrl = "https://storage.example/quote-v2.pdf",
                PdfGeneratedAt = new DateTime(2026, 4, 18, 14, 30, 0, DateTimeKind.Utc),
                GeneratedByDisplayName = "Alex Kim",
                CreatedBy = "Alex Kim",
                CreatedAt = new DateTime(2026, 4, 18, 14, 22, 0, DateTimeKind.Utc)
            }
        ]
    };

    private ProjectProductionPlanDto BuildProductionPlan()
    {
        var proposedStart = new DateTimeOffset(2026, 4, 19, 9, 0, 0, TimeSpan.Zero);
        var proposedEnd = proposedStart.AddHours(3);
        var schedule = new List<PlanningScheduleItemDto>
        {
            new(
                proposedStart.AddHours(-5),
                proposedStart.AddHours(-3),
                "JOB-1001",
                "Queued",
                Guid.Parse("12121212-1212-1212-1212-121212121212"),
                "CNC Mill 01",
                60,
                120),
            new(
                proposedStart.AddHours(-3),
                proposedStart.AddHours(-1),
                "HOLD-AAAA",
                "ActiveHold",
                Guid.Parse("34343434-3434-3434-3434-343434343434"),
                "CNC Mill 01",
                60,
                120,
                IsHold: true,
                HoldId: _planningHoldId)
        };

        var cncRouting = new ProductionRoutingDto(
            _machineId,
            "CNC-01",
            "CNC Mill 01",
            2,
            proposedStart,
            schedule,
            proposedStart,
            proposedEnd);

        var fdmRouting = new ProductionRoutingDto(
            Guid.Parse("abababab-abab-abab-abab-abababababab"),
            "FDM-01",
            "FDM Printer 01",
            1,
            proposedStart.AddHours(2),
            [],
            proposedStart.AddHours(2),
            proposedStart.AddHours(6));

        return new ProjectProductionPlanDto
        {
            ProjectId = _projectId,
            ScheduleBoard = BuildProductionScheduleBoard(proposedStart, proposedEnd),
            Parts =
            [
                new ProjectProductionPartPlanDto
                {
                    PartId = _bracketPartId,
                    FileName = "bracket-left.stl",
                    ThumbnailUrl = "https://storage.example/bracket-thumb.webp",
                    Dimensions = "120 x 64 x 18 mm",
                    ProcessType = "CNC_MILL",
                    MaterialName = "Aluminium 6061-T6",
                    Configuration = "Anodized - Black - ISO 2768-m",
                    Quantity = 4,
                    DfmStatus = "Requires acknowledgement",
                    Routing = cncRouting,
                    CanCreateHold = true
                },
                new ProjectProductionPartPlanDto
                {
                    PartId = _sensorPartId,
                    FileName = "sensor-cover.3mf",
                    Dimensions = "42 x 22 x 12 mm",
                    ProcessType = "FDM",
                    MaterialName = "PA12 Nylon",
                    Configuration = "As printed - Standard FDM settings",
                    Quantity = 15,
                    DfmStatus = "DFM passed",
                    Routing = fdmRouting,
                    ActiveHold = BuildPlanningHold(_sensorPartId, 3),
                    CanCreateHold = true
                },
                new ProjectProductionPartPlanDto
                {
                    PartId = _fixturePartId,
                    FileName = "fixture-base.step",
                    Dimensions = "80 x 48 x 12 mm",
                    ProcessType = "CNC_MILL",
                    MaterialName = "Aluminium 6061-T6",
                    Configuration = "Standard settings",
                    Quantity = 2,
                    DfmStatus = "DFM acknowledged",
                    Routing = cncRouting,
                    JobId = _jobId,
                    JobStatus = "InProduction",
                    MachineName = "CNC Mill 01",
                    CanCreateHold = false,
                    HoldBlockReason = "Production job already exists."
                }
            ]
        };
    }

    private ProductionScheduleBoardDto BuildProductionScheduleBoard(DateTimeOffset proposedStart, DateTimeOffset proposedEnd) => new()
    {
        RangeStart = proposedStart.AddDays(-1).UtcDateTime.Date,
        RangeEnd = proposedStart.AddDays(6).UtcDateTime.Date,
        Machines =
        [
            new ProductionScheduleMachineDto
            {
                MachineId = "CNC-01",
                MachineName = "CNC Mill 01",
                Category = "CncMachine",
                Technology = "CNC_MILL",
                Slots =
                [
                    new ProductionScheduleSlotDto
                    {
                        SlotId = Guid.Parse("12121212-1212-1212-1212-121212121212"),
                        JobId = Guid.Parse("12121212-1212-1212-1212-121212121212"),
                        ProjectId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                        MachineId = "CNC-01",
                        MachineName = "CNC Mill 01",
                        Technology = "CNC_MILL",
                        ScheduledStart = proposedStart.AddHours(-5).UtcDateTime,
                        ScheduledEnd = proposedStart.AddHours(-3).UtcDateTime,
                        SetupMinutes = 60,
                        ProductionMinutes = 120,
                        QueuePosition = 1,
                        Status = "Queued",
                        Label = "JOB-1001",
                        CanMove = true
                    },
                    new ProductionScheduleSlotDto
                    {
                        SlotId = _planningHoldId,
                        HoldId = _planningHoldId,
                        ProjectId = _projectId,
                        ProjectPartId = _bracketPartId,
                        FileName = "bracket-left.stl",
                        MachineId = "CNC-01",
                        MachineName = "CNC Mill 01",
                        Technology = "CNC_MILL",
                        ScheduledStart = proposedStart.AddHours(-3).UtcDateTime,
                        ScheduledEnd = proposedStart.AddHours(-1).UtcDateTime,
                        SetupMinutes = 60,
                        ProductionMinutes = 120,
                        QueuePosition = 3,
                        Status = "ActiveHold",
                        Label = "HOLD-AAAA",
                        ExpiresAt = proposedStart.AddDays(3).UtcDateTime,
                        IsHold = true,
                        IsCurrentProject = true,
                        CanMove = true
                    },
                    new ProductionScheduleSlotDto
                    {
                        SlotId = Guid.Parse("91919191-9191-9191-9191-919191919191"),
                        ProjectId = _projectId,
                        ProjectPartId = _bracketPartId,
                        FileName = "bracket-left.stl",
                        MachineId = "CNC-01",
                        MachineName = "CNC Mill 01",
                        Technology = "CNC_MILL",
                        ScheduledStart = proposedStart.UtcDateTime,
                        ScheduledEnd = proposedEnd.UtcDateTime,
                        SetupMinutes = 60,
                        ProductionMinutes = 120,
                        QueuePosition = 4,
                        Status = "Proposed",
                        Label = "fixture-base.step",
                        IsProposed = true,
                        IsCurrentProject = true
                    }
                ]
            },
            new ProductionScheduleMachineDto
            {
                MachineId = "FDM-01",
                MachineName = "FDM Printer 01",
                Category = "FdmPrinter",
                Technology = "FDM",
                Slots =
                [
                    new ProductionScheduleSlotDto
                    {
                        SlotId = Guid.Parse("92929292-9292-9292-9292-929292929292"),
                        HoldId = _planningHoldId,
                        ProjectId = _projectId,
                        ProjectPartId = _sensorPartId,
                        FileName = "sensor-cover.3mf",
                        MachineId = "FDM-01",
                        MachineName = "FDM Printer 01",
                        Technology = "FDM",
                        ScheduledStart = proposedStart.AddHours(2).UtcDateTime,
                        ScheduledEnd = proposedStart.AddHours(6).UtcDateTime,
                        SetupMinutes = 30,
                        ProductionMinutes = 210,
                        QueuePosition = 3,
                        Status = "ActiveHold",
                        Label = "sensor-cover.3mf",
                        ExpiresAt = proposedStart.AddDays(3).UtcDateTime,
                        IsHold = true,
                        IsCurrentProject = true,
                        CanMove = true
                    }
                ]
            }
        ]
    };

    private ProductionPlanningHoldDto BuildPlanningHold(Guid partId, int queuePosition)
    {
        var isFdm = partId == _sensorPartId;
        return new ProductionPlanningHoldDto
        {
            Id = _planningHoldId,
            ProjectId = _projectId,
            ProjectPartId = partId,
            Technology = isFdm ? "FDM" : "CNC_MILL",
            MachineId = isFdm ? "FDM-01" : "CNC-01",
            MachineName = isFdm ? "FDM Printer 01" : "CNC Mill 01",
            QueuePosition = queuePosition,
            ScheduledStartTime = new DateTime(2026, 4, 19, 9, 0, 0, DateTimeKind.Utc),
            ScheduledEndTime = new DateTime(2026, 4, 19, 12, 0, 0, DateTimeKind.Utc),
            SetupTimeMinutes = isFdm ? 30 : 60,
            ProductionTimeMinutes = isFdm ? 210 : 120,
            Quantity = isFdm ? 15 : 4,
            Status = "Active",
            Notes = "Planned from project PRJ-2026-0184",
            CreatedBy = "employee:alex.kim",
            CreatedAt = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2026, 4, 21, 15, 0, 0, DateTimeKind.Utc)
        };
    }

    private static Task<HttpResponseMessage> Json<T>(T body) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(body)
        });

    private static string FindProjectDetailCssPath()
    {
        var workingDirectoryCandidate = Path.Combine(
            Environment.CurrentDirectory,
            "Maliev.Intranet.Client",
            "Pages",
            "ProjectDetail.razor.css");

        if (File.Exists(workingDirectoryCandidate))
            return workingDirectoryCandidate;

        var workspaceCandidate = @"B:\maliev\Maliev.Intranet\Maliev.Intranet.Client\Pages\ProjectDetail.razor.css";
        if (File.Exists(workspaceCandidate))
            return workspaceCandidate;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var sourceCandidate = Path.Combine(
                directory.FullName,
                "Maliev.Intranet.Client",
                "Pages",
                "ProjectDetail.razor.css");

            if (File.Exists(sourceCandidate))
                return sourceCandidate;

            var repoCandidate = Path.GetFullPath(Path.Combine(
                directory.FullName,
                "..",
                "..",
                "..",
                "..",
                "..",
                "Maliev.Intranet",
                "Maliev.Intranet.Client",
                "Pages",
                "ProjectDetail.razor.css"));

            if (File.Exists(repoCandidate))
                return repoCandidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate ProjectDetail.razor.css from the test output directory.");
    }
}
