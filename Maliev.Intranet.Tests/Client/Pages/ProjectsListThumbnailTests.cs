using System.Net;
using System.Net.Http.Json;
using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Component tests for project list part thumbnail previews.
/// </summary>
public class ProjectsListThumbnailTests : BunitContext
{
    public ProjectsListThumbnailTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/projects")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new PagedResponse<ProjectSummaryDto>
                    {
                        Data =
                        [
                            new ProjectSummaryDto
                            {
                                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                                ProjectNumber = "PRJ-2026-0003",
                                CustomerName = "Pimchanok Garcia",
                                Title = "Project 2026-06-01",
                                Status = "Configuring",
                                PartsCount = 4,
                                CreatedAt = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc),
                                PartPreviews =
                                [
                                    new ProjectPartPreviewDto
                                    {
                                        Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                                        PartNumber = 1,
                                        FileName = "fixture.step",
                                        ThumbnailUrl = "https://signed.example/fixture_small.webp",
                                        ProcessType = "CNC_Milling",
                                        MaterialName = "Aluminium 6061",
                                        Quantity = 1
                                    },
                                    new ProjectPartPreviewDto
                                    {
                                        Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                                        PartNumber = 2,
                                        FileName = "cover.stl",
                                        ProcessType = "FDM",
                                        Quantity = 3
                                    },
                                    new ProjectPartPreviewDto
                                    {
                                        Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                                        PartNumber = 3,
                                        FileName = "hinge.step",
                                        ThumbnailUrl = "https://signed.example/hinge_small.webp",
                                        ProcessType = "CNC_Milling",
                                        MaterialName = "Brass",
                                        Quantity = 2
                                    },
                                    new ProjectPartPreviewDto
                                    {
                                        Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                                        PartNumber = 4,
                                        FileName = "spacer.stl",
                                        ThumbnailUrl = "https://signed.example/spacer_small.webp",
                                        ProcessType = "SLA",
                                        Quantity = 6
                                    }
                                ]
                            }
                        ],
                        Meta = new PaginationMeta
                        {
                            CurrentPage = 1,
                            PageSize = 20,
                            TotalCount = 1,
                            TotalItems = 1,
                            TotalPages = 1
                        }
                    })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://test/") });
    }

    [Fact]
    public void ProjectsPage_WhenProjectsHavePartPreviews_RendersThumbnailStrip()
    {
        var cut = Render<Projects>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("project-part-preview-strip", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("https://signed.example/fixture_small.webp", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("fixture.step thumbnail", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("cover.stl", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ProjectsPage_WhenProjectsHavePartPreviews_RendersThumbnailsBelowProjectName()
    {
        var cut = Render<Projects>();

        cut.WaitForAssertion(() =>
        {
            var copyIndex = cut.Markup.IndexOf("project-row-copy", StringComparison.Ordinal);
            var previewIndex = cut.Markup.IndexOf("project-part-preview-strip", StringComparison.Ordinal);

            Assert.True(copyIndex >= 0);
            Assert.True(previewIndex >= 0);
            Assert.True(copyIndex < previewIndex);
        });
    }

    [Fact]
    public void ProjectsPage_WhenProjectHasHiddenPartPreviews_RendersOverflowPopoutWithAllThumbnails()
    {
        var cut = Render<Projects>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("project-part-thumb-count", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("+1", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("project-part-overflow-popout", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("All part previews", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("fixture.step", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("cover.stl", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("hinge.step", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("spacer.stl", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ProjectsPageCss_DefinesThumbnailHoverPreviewAndOverflowPopout()
    {
        var css = File.ReadAllText(FindProjectsCssPath());

        Assert.Contains(".project-part-thumb:hover .project-part-thumb-preview", css, StringComparison.Ordinal);
        Assert.Contains(".project-part-thumb:focus-within .project-part-thumb-preview", css, StringComparison.Ordinal);
        Assert.Contains(".project-part-thumb-count:hover .project-part-overflow-popout", css, StringComparison.Ordinal);
        Assert.Contains(".project-part-thumb-count:focus-within .project-part-overflow-popout", css, StringComparison.Ordinal);
    }

    private static string FindProjectsCssPath()
    {
        var workingDirectoryCandidate = Path.Combine(
            Environment.CurrentDirectory,
            "Maliev.Intranet.Client",
            "Pages",
            "Projects.razor.css");

        if (File.Exists(workingDirectoryCandidate))
            return workingDirectoryCandidate;

        var workspaceCandidate = @"B:\maliev\Maliev.Intranet\Maliev.Intranet.Client\Pages\Projects.razor.css";
        if (File.Exists(workspaceCandidate))
            return workspaceCandidate;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var sourceCandidate = Path.Combine(
                directory.FullName,
                "Maliev.Intranet.Client",
                "Pages",
                "Projects.razor.css");

            if (File.Exists(sourceCandidate))
                return sourceCandidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate Projects.razor.css from the test output directory.");
    }
}
