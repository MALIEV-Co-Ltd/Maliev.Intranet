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
                                PartsCount = 2,
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
}
