using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maliev.Intranet.Tests.Bff.Services;

public class GlobalSearchResultEnricherTests
{
    [Fact]
    public async Task ToGlobalSearchResponseAsync_ForProjectPart_AddsPartThumbnail()
    {
        var projectId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        var thumbnailUrl = "https://storage.example/d11-12.webp";
        var projectClient = CreateProjectClient(projectId, partId, thumbnailUrl);
        var uploadClient = CreateUploadClient(_ =>
            throw new InvalidOperationException("UploadService should not be called when ProjectService returns a thumbnail URL."));
        var enricher = new GlobalSearchResultEnricher(projectClient, uploadClient, NullLogger<GlobalSearchResultEnricher>.Instance);
        var response = new SearchServiceResponseDto(
            "d11",
            1,
            [
                new SearchServiceResultDto(
                    "ProjectService",
                    "project-part",
                    $"{projectId}:{partId}",
                    "d11-12.stp",
                    "PRJ-2026-0001 - FDM - PETG",
                    null,
                    "Quoted",
                    "project.projects.read",
                    1.0d,
                    DateTimeOffset.UtcNow)
            ]);

        var mapped = await enricher.ToGlobalSearchResponseAsync(response, "d11", CancellationToken.None);

        var result = Assert.Single(mapped.Results);
        Assert.Equal(thumbnailUrl, result.ThumbnailUrl);
    }

    [Fact]
    public async Task ToGlobalSearchResponseAsync_ForProjectPartWithOnlyStoragePath_ResolvesSignedThumbnail()
    {
        var projectId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        const string storagePath = "customers/demo/projects/PRJ-2026-0001/thumb-small.webp";
        const string signedUrl = "https://signed.example/thumb-small.webp";
        var projectClient = CreateProjectClient(projectId, partId, thumbnailUrl: null, thumbnailSmallGcsPath: storagePath);
        var uploadClient = CreateUploadClient(request =>
        {
            Assert.Equal("/upload/v1/files/by-path/signed-url", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl })
            };
        });
        var enricher = new GlobalSearchResultEnricher(projectClient, uploadClient, NullLogger<GlobalSearchResultEnricher>.Instance);
        var response = new SearchServiceResponseDto(
            "d11",
            1,
            [
                new SearchServiceResultDto(
                    "ProjectService",
                    "project-part",
                    $"{projectId}:{partId}",
                    "d11-12.stp",
                    null,
                    null,
                    "Quoted",
                    "project.projects.read",
                    1.0d,
                    DateTimeOffset.UtcNow)
            ]);

        var mapped = await enricher.ToGlobalSearchResponseAsync(response, "d11", CancellationToken.None);

        var result = Assert.Single(mapped.Results);
        Assert.Equal(signedUrl, result.ThumbnailUrl);
    }

    private static ProjectServiceClient CreateProjectClient(
        Guid projectId,
        Guid partId,
        string? thumbnailUrl,
        string? thumbnailSmallGcsPath = null)
    {
        return new ProjectServiceClient(new HttpClient(new MockHttpMessageHandler((request, _) =>
        {
            Assert.Equal($"/project/v1/projects/{projectId}", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = projectId,
                    projectNumber = "PRJ-2026-0001",
                    customerId = Guid.NewGuid(),
                    customerName = "Pimchanok Garcia",
                    title = "Project 2026-05-06",
                    status = "QuotationGenerated",
                    totalEstimatedPrice = 0m,
                    totalPrice = 0m,
                    currency = "THB",
                    createdAt = DateTime.UtcNow,
                    parts = new[]
                    {
                        new
                        {
                            id = partId,
                            fileName = "d11-12.stp",
                            thumbnailUrl,
                            thumbnailSmallGcsPath,
                            processType = "FDM",
                            materialName = "PETG",
                            quantity = 12,
                            status = "Quoted"
                        }
                    }
                })
            });
        }))
        {
            BaseAddress = new Uri("http://project-test")
        });
    }

    private static UploadServiceClient CreateUploadClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        return new UploadServiceClient(new HttpClient(new MockHttpMessageHandler((request, _) =>
            Task.FromResult(handler(request))))
        {
            BaseAddress = new Uri("http://upload-test")
        });
    }
}
