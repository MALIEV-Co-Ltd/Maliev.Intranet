using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using System.Net;
using System.Net.Http.Json;

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
    private bool _notePosted;

    public ProjectDetailPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

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
        Assert.Contains("aria-label=\"Generate PDF\"", cut.Markup);
        Assert.Contains("project-header-icon-action", cut.Markup);
        Assert.Contains("Accept quote", cut.Markup);

        var expectedLocalUpdated = new DateTime(2026, 4, 18, 14, 22, 0, DateTimeKind.Utc)
            .ToLocalTime()
            .ToString("MMM d, yyyy HH:mm");
        var headerMeta = cut.Find(".mlv-page-meta").TextContent;
        Assert.Contains($"Robot arm calibration fixture - Axion Robotics • Last updated on {expectedLocalUpdated}", headerMeta);
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
    public void ProjectDetail_QuoteActions_CallExistingBffEndpoints()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("project-action-download", cut.Markup));
        cut.Find("button.project-action-download").Click();
        cut.Find("button.project-action-accept").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedPaths, path => path == $"/api/v1/quotations/{_quotationId}/pdf");
            Assert.DoesNotContain(_requestedPaths, path => path == $"/api/v1/projects/{_projectId}/accept-quotation");
        });

        cut.Find("button[data-tab='quote']").Click();
        cut.WaitForAssertion(() => Assert.Contains("https://storage.example/quote.pdf", cut.Markup));

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
        Assert.Contains("Commercial breakdown", cut.Markup);
        Assert.Contains("No PDF generated in this session", cut.Markup);
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
    public void ProjectDetail_PlanningTab_RendersRoutingQueueHoldsAndJobs()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Planning", cut.Markup));
        cut.Find("button[data-tab='planning']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Production planning", cut.Markup));

        Assert.Contains(_requestedPaths, path => path == $"/api/v1/projects/{_projectId}/production-plan");
        Assert.Contains("project-planning-panel", cut.Markup);
        Assert.Contains("project-planning-table", cut.Markup);
        Assert.Contains("3 quoted parts", cut.Markup);
        Assert.Contains("Manufacturing", cut.Markup);
        Assert.Contains("Qty / DFM", cut.Markup);
        Assert.Contains("bracket-left.stl", cut.Markup);
        Assert.Contains("CNC Milling", cut.Markup);
        Assert.Contains("Aluminium 6061-T6", cut.Markup);
        Assert.Contains("Anodized - Black - ISO 2768-m", cut.Markup);
        Assert.Contains("Requires acknowledgement", cut.Markup);
        Assert.Contains("CNC Mill 01", cut.Markup);
        Assert.Contains("2 queued ahead", cut.Markup);
        Assert.Contains("Hold #3", cut.Markup);
        Assert.Contains("CCCCCCCC", cut.Markup);
        Assert.Contains("Preview routing", cut.Markup);
        Assert.Contains("Create planning hold", cut.Markup);
        Assert.Contains("Update planning hold", cut.Markup);
        Assert.Contains("Cancel planning hold", cut.Markup);
        Assert.Contains("Open job", cut.Markup);
    }

    [Fact]
    public void ProjectDetail_PlanningActions_CallPlanningEndpoints()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Planning", cut.Markup));
        cut.Find("button[data-tab='planning']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Production planning", cut.Markup));

        cut.Find("button[aria-label='Create planning hold']").Click();
        cut.WaitForAssertion(() =>
            Assert.Contains(_requestedRequests, request => request == $"POST /api/v1/projects/{_projectId}/parts/{_bracketPartId}/planning-hold"));

        cut.Find("button[aria-label='Update planning hold']").Click();
        cut.WaitForAssertion(() =>
            Assert.Contains(_requestedRequests, request => request == $"PATCH /api/v1/projects/{_projectId}/planning-holds/{_planningHoldId}"));

        cut.Find("button[aria-label='Cancel planning hold']").Click();
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
        Assert.Contains("Standard FDM settings", cut.Markup);
        Assert.Contains("project-config-primary", cut.Markup);
        Assert.Contains("project-config-subtitle", cut.Markup);
        Assert.DoesNotContain("AS_PRINTED", cut.Markup);
        Assert.DoesNotContain("FDM_STD", cut.Markup);
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
        Assert.Contains("DFM acknowledged", cut.Markup);
        Assert.Contains("Warnings reviewed", cut.Markup);
        Assert.Contains("DFM passed", cut.Markup);
        Assert.Contains("No reported issues", cut.Markup);
        Assert.DoesNotContain("DFM pending", cut.Markup);
        Assert.DoesNotContain("Awaiting review", cut.Markup);
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
        Assert.Contains("::deep .project-header-subtitle", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-header-icon-action", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-field-grid", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-metric-row", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-parts-table", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-config-stack", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-dfm-copy", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-notes-grid", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-note-audit", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-overview-sidebar", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-manufacturing-card", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-field-grid-compact", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-snapshot-compact", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-quote-terms", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-customer-heading", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-customer-detail-grid", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-customer-address-block", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-quote-workspace", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-document-preview", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-commercial-breakdown", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-commercial-totals", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-planning-panel", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-planning-table", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-planning-actions", css, StringComparison.Ordinal);
        Assert.Contains("th:nth-child(6)", css, StringComparison.Ordinal);
        Assert.Contains("th:nth-child(7)", css, StringComparison.Ordinal);
        Assert.Contains("text-align: right", css, StringComparison.Ordinal);
        Assert.Contains("width: 5%", css, StringComparison.Ordinal);
        Assert.Contains("width: 100%", css, StringComparison.Ordinal);
        Assert.Contains("position: sticky", css, StringComparison.Ordinal);
        Assert.Contains("height: 52px", css, StringComparison.Ordinal);
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
                        Tolerance = "ISO 2768-m",
                        Quantity = 4,
                        ConfirmedPrice = 2500m,
                        Status = "Confirmed",
                        ThumbnailUrl = "https://storage.example/bracket-thumb.webp",
                        HasDfmWarnings = true,
                        DfmAcknowledged = false,
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

        if (pathAndQuery.Equals($"/api/v1/projects/{_projectId}/production-plan", StringComparison.Ordinal))
        {
            return Json(BuildProductionPlan());
        }

        if (request.Method == HttpMethod.Post
            && pathAndQuery.Equals($"/api/v1/projects/{_projectId}/parts/{_bracketPartId}/planning-hold", StringComparison.Ordinal))
        {
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

        if (pathAndQuery.Equals($"/api/v1/quotations/{_quotationId}/pdf", StringComparison.Ordinal))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("https://storage.example/quote.pdf")
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
        SubTotal = 18250m,
        Total = 18250m,
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
                TotalPrice = 18250m,
                CurrencyCode = "THB",
                DeliveryExpectations = "7-10 business days",
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

    private ProductionPlanningHoldDto BuildPlanningHold(Guid partId, int queuePosition) => new()
    {
        Id = _planningHoldId,
        ProjectId = _projectId,
        ProjectPartId = partId,
        Technology = "CNC_MILL",
        MachineId = "CNC-01",
        MachineName = "CNC Mill 01",
        QueuePosition = queuePosition,
        ScheduledStartTime = new DateTime(2026, 4, 19, 9, 0, 0, DateTimeKind.Utc),
        ScheduledEndTime = new DateTime(2026, 4, 19, 12, 0, 0, DateTimeKind.Utc),
        SetupTimeMinutes = 60,
        ProductionTimeMinutes = 120,
        Quantity = 4,
        Status = "Active",
        Notes = "Planned from project PRJ-2026-0184",
        CreatedBy = "employee:alex.kim",
        CreatedAt = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc),
        ExpiresAt = new DateTime(2026, 4, 21, 15, 0, 0, DateTimeKind.Utc)
    };

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
