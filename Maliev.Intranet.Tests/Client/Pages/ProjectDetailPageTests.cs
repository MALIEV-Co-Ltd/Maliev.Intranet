using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.Intranet.Tests.Client.Pages;

public sealed class ProjectDetailPageTests : BunitContext, IAsyncLifetime
{
    private readonly Guid _projectId = Guid.Parse("5daabfe7-7b4a-43fe-9287-b596eb75ece8");
    private readonly Guid _quotationId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private readonly List<string> _requestedPaths = [];
    private bool _notePosted;

    public ProjectDetailPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler(HandleRequestAsync);
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://test/") });
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
        Assert.Contains("Timeline", cut.Markup);
        Assert.Contains("Customer", cut.Markup);
        Assert.Contains("Axion Robotics", cut.Markup);
        Assert.Contains("QT-2026-0098", cut.Markup);
        Assert.Contains("Manufacturing summary", cut.Markup);
        Assert.Contains("bracket-left.stl", cut.Markup);
        Assert.Contains("18,250.00", cut.Markup);
        Assert.Contains("Edit project", cut.Markup);
        Assert.Contains("Download quote", cut.Markup);
        Assert.Contains("Accept quote", cut.Markup);
    }

    [Fact]
    public void ProjectDetail_EditProject_NavigatesBackToProjectNewResumeRoute()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Edit project", cut.Markup));
        cut.Find("button.project-action-edit").Click();

        Assert.EndsWith($"/sales/projects/new?resume={_projectId}", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectDetail_QuoteActions_CallExistingBffEndpoints()
    {
        var cut = Render<ProjectDetail>(parameters => parameters.Add(page => page.Id, _projectId));

        cut.WaitForAssertion(() => Assert.Contains("Download quote", cut.Markup));
        cut.Find("button.project-action-download").Click();
        cut.Find("button.project-action-accept").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedPaths, path => path == $"/api/v1/quotations/{_quotationId}/pdf");
            Assert.DoesNotContain(_requestedPaths, path => path == $"/api/v1/projects/{_projectId}/accept-quotation");
        });

        cut.Find("button.project-accept-confirm").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedPaths, path => path == $"/api/v1/projects/{_projectId}/accept-quotation");
            Assert.Contains("Accepted", cut.Markup);
        });
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

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedPaths, path => path == $"/api/v1/projects/{_projectId}/notes");
            Assert.Contains("Saved", cut.Markup);
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
        Assert.Contains("::deep .project-field-grid", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-metric-row", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-parts-table", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-config-stack", css, StringComparison.Ordinal);
        Assert.Contains("::deep .project-dfm-copy", css, StringComparison.Ordinal);
        Assert.Contains("position: sticky", css, StringComparison.Ordinal);
        Assert.Contains("height: 52px", css, StringComparison.Ordinal);
    }

    private Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request, CancellationToken _)
    {
        var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;
        _requestedPaths.Add(pathAndQuery);

        if (pathAndQuery.Equals($"/api/v1/projects/{_projectId}", StringComparison.Ordinal))
        {
            return Json(new ProjectDetailDto
            {
                Id = _projectId,
                ProjectNumber = "PRJ-2026-0184",
                CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                CustomerName = "Axion Robotics",
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
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
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
                        Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
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
                        Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
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
                            Content = "Check customer's drawing revision before release.",
                            CreatedAt = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)
                        }
                    ]
                    : []
            });
        }

        if (pathAndQuery.Equals($"/api/v1/quotations/{_quotationId}/pdf", StringComparison.Ordinal))
        {
            return Json("https://storage.example/quote.pdf");
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
                Content = "Check customer's drawing revision before release.",
                CreatedAt = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
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
