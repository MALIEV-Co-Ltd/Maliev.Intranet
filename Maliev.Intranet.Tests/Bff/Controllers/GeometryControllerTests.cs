using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class GeometryControllerTests
{
    private const string UploadId = "test-upload-id";
    private const string ProcessCode = "FDM";
    private const string StoragePath = "customers/c1/projects/p1/part.stl";
    private const string SignedUrl = "https://storage.googleapis.com/signed";

    private static UploadServiceClient MakeUploadClient(
        string? storagePath = StoragePath,
        string? signedUrl = SignedUrl)
    {
        return new UploadServiceClient(new HttpClient(new MockHttpMessageHandler((req, _) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains($"/files/{UploadId}"))
            {
                if (storagePath == null)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { storagePath })
                });
            }

            if (req.RequestUri.PathAndQuery.Contains("by-path/signed-url"))
            {
                if (signedUrl == null)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Gone)
                    {
                        Content = JsonContent.Create(new { error = "file_missing" })
                    });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { signedUrl })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }))
        { BaseAddress = new Uri("http://test") });
    }

    private static GeometryServiceClient MakeGeometryClient(DfmAnalysisResponse response)
    {
        return new GeometryServiceClient(new HttpClient(new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response, options: new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                })
            })))
        { BaseAddress = new Uri("http://test") });
    }

    private static GeometryController MakeController(UploadServiceClient uploadClient, GeometryServiceClient geometryClient)
        => new(geometryClient, uploadClient, NullLogger<GeometryController>.Instance);

    [Fact]
    public async Task AnalyzeForProcess_Returns410_WhenFileNotInUploadService()
    {
        var controller = MakeController(
            MakeUploadClient(storagePath: null),
            MakeGeometryClient(new DfmAnalysisResponse { Status = "analysis_complete" }));

        var result = await controller.AnalyzeForProcess(UploadId, ProcessCode, new(), default);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(410, objectResult.StatusCode);
        var body = Assert.IsType<DfmAnalysisResponse>(objectResult.Value);
        Assert.Equal("file_missing", body.Status);
        Assert.Equal(UploadId, body.UploadId);
    }

    [Fact]
    public async Task AnalyzeForProcess_Returns410_WhenSignedUrlGenerationFails()
    {
        var controller = MakeController(
            MakeUploadClient(storagePath: StoragePath, signedUrl: null),
            MakeGeometryClient(new DfmAnalysisResponse { Status = "analysis_complete" }));

        var result = await controller.AnalyzeForProcess(UploadId, ProcessCode, new(), default);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(410, objectResult.StatusCode);
        var body = Assert.IsType<DfmAnalysisResponse>(objectResult.Value);
        Assert.Equal("file_missing", body.Status);
    }

    [Fact]
    public async Task AnalyzeForProcess_SucceedsWithStaleClientPath_WhenUploadServiceHasCurrentPath()
    {
        // Even when the client sends a stale path (e.g. from before migration),
        // the controller resolves the current path from UploadService and reaches the geometry service.
        var controller = MakeController(
            MakeUploadClient(storagePath: "customers/NEW/path.stl", signedUrl: SignedUrl),
            MakeGeometryClient(new DfmAnalysisResponse
            {
                UploadId = UploadId,
                ProcessCode = ProcessCode,
                Status = "analysis_complete",
                DfmReport = new() { ReportType = ProcessCode }
            }));

        var clientRequest = new GeometryAnalysisRequest { StoragePath = "projects/STALE/path.stl" };
        var result = await controller.AnalyzeForProcess(UploadId, ProcessCode, clientRequest, default);

        // The controller reached the geometry service and returned its result — not 410
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DfmAnalysisResponse>(okResult.Value);
        Assert.Equal("analysis_complete", body.Status);
    }
}
