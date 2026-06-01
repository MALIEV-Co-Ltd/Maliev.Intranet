using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>
/// Tests for project summary preview enrichment in the BFF list endpoint.
/// </summary>
public class ProjectsControllerSummaryPreviewTests
{
    [Fact]
    public async Task Get_WhenSummaryPreviewHasRawThumbnailPath_ReturnsSignedThumbnailUrl()
    {
        var projectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var partId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var projectHandler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    data = new[]
                    {
                        new
                        {
                            id = projectId,
                            projectNumber = "PRJ-2026-0003",
                            customerName = "Pimchanok Garcia",
                            title = "Project 2026-06-01",
                            status = "Configuring",
                            partsCount = 1,
                            totalEstimatedPrice = 0m,
                            createdAt = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc),
                            partPreviews = new[]
                            {
                                new
                                {
                                    id = partId,
                                    partNumber = 1,
                                    fileName = "fixture.step",
                                    thumbnailSmallGcsPath = "customers/c1/projects/p1/source/fixture_small.webp",
                                    processType = "CNC_Milling",
                                    materialName = "Aluminium 6061",
                                    quantity = 1
                                }
                            }
                        }
                    },
                    currentPage = 1,
                    pageSize = 20,
                    totalCount = 1,
                    totalPages = 1
                })
            }));
        var uploadHandler = new MockHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/upload/v1/files/by-path/signed-url", request.RequestUri!.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl = "https://signed.example/fixture_small.webp" })
            });
        });
        var controller = new ProjectsController(
            new ProjectServiceClient(new HttpClient(projectHandler) { BaseAddress = new Uri("http://project") }),
            new JobServiceClient(new HttpClient(new MockHttpMessageHandler()) { BaseAddress = new Uri("http://job") }),
            Mock.Of<IFacilityServiceClient>(),
            NullLogger<ProjectsController>.Instance,
            new UploadServiceClient(new HttpClient(uploadHandler) { BaseAddress = new Uri("http://upload") }));

        var action = await controller.Get(status: "Configuring", ct: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var response = Assert.IsType<PagedResponse<ProjectSummaryDto>>(ok.Value);
        var project = Assert.Single(response.Data);
        var preview = Assert.Single(project.PartPreviews);
        Assert.Equal("https://signed.example/fixture_small.webp", preview.ThumbnailUrl);
    }
}
