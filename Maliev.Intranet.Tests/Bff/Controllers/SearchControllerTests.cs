using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class SearchControllerTests
{
    [Fact]
    public async Task Search_WhenQueryBelowMinimum_ReturnsEmptyResponseWithoutCallingSearchService()
    {
        var client = new SearchServiceClient(new HttpClient(new MockHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("SearchService should not be called for short queries.")))
        {
            BaseAddress = new Uri("http://test")
        });
        var controller = new SearchController(client, CreateThrowingEnricher());

        var result = await controller.Search("a");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GlobalSearchResponseDto>(ok.Value);
        Assert.Equal("a", response.Query);
        Assert.Empty(response.Results);
    }

    [Fact]
    public async Task Search_WhenSearchServiceReturnsRows_MapsToGlobalSearchResponse()
    {
        var projectId = Guid.NewGuid();
        var client = new SearchServiceClient(new HttpClient(new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SearchServiceResponseDto(
                    "fixture",
                    1,
                    [
                        new SearchServiceResultDto(
                            "ProjectService",
                            "project",
                            projectId.ToString(),
                            "Fixture",
                            "Customer",
                            null,
                            "Draft",
                            "project.projects.read",
                            0.91d,
                            DateTimeOffset.UtcNow)
                    ]))
            })))
        {
            BaseAddress = new Uri("http://test")
        });
        var controller = new SearchController(client, CreateThrowingEnricher());

        var result = await controller.Search("fixture", limit: 5);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GlobalSearchResponseDto>(ok.Value);
        var row = Assert.Single(response.Results);
        Assert.Equal("Fixture", row.Title);
        Assert.Equal("Sales & CRM", row.Area);
        Assert.Equal($"/sales/projects/{projectId}", row.Href);
    }

    private static GlobalSearchResultEnricher CreateThrowingEnricher()
    {
        var projectClient = new ProjectServiceClient(new HttpClient(new MockHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("ProjectService should not be called for this test.")))
        {
            BaseAddress = new Uri("http://project-test")
        });
        var uploadClient = new UploadServiceClient(new HttpClient(new MockHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("UploadService should not be called for this test.")))
        {
            BaseAddress = new Uri("http://upload-test")
        });

        return new GlobalSearchResultEnricher(projectClient, uploadClient, NullLogger<GlobalSearchResultEnricher>.Instance);
    }
}
