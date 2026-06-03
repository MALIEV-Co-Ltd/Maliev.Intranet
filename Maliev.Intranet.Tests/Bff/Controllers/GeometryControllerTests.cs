using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Http;
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

    private static GeometryServiceClient MakeGeometryClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        return new GeometryServiceClient(new HttpClient(new MockHttpMessageHandler(handler))
        { BaseAddress = new Uri("http://test") });
    }

    private static GeometryController MakeController(UploadServiceClient uploadClient, GeometryServiceClient geometryClient)
    {
        return new(geometryClient, uploadClient, NullLogger<GeometryController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

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
    public async Task AnalyzeForProcess_UsesClientStoragePathFallback_WhenUploadMetadataMissing()
    {
        string? geometryRequestJson = null;
        var controller = MakeController(
            MakeUploadClient(storagePath: null, signedUrl: SignedUrl),
            MakeGeometryClient(async (req, _) =>
            {
                geometryRequestJson = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new DfmAnalysisResponse
                    {
                        UploadId = UploadId,
                        ProcessCode = ProcessCode,
                        Status = "analysis_complete",
                        DfmReport = new() { ReportType = ProcessCode }
                    }, options: new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    })
                };
            }));

        var result = await controller.AnalyzeForProcess(
            UploadId,
            ProcessCode,
            new GeometryAnalysisRequest { StoragePath = StoragePath },
            default);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DfmAnalysisResponse>(okResult.Value);
        Assert.Equal("analysis_complete", body.Status);
        Assert.NotNull(geometryRequestJson);

        using var document = JsonDocument.Parse(geometryRequestJson);
        Assert.Equal(StoragePath, document.RootElement.GetProperty("storage_path").GetString());
        Assert.Equal(SignedUrl, document.RootElement.GetProperty("download_url").GetString());
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

    [Fact]
    public async Task AnalyzeForProcess_ReResolvesStoragePath_WhenSignedUrlMissesDuringMigration()
    {
        const string stalePath = "projects/p1/part.stl";
        const string currentPath = "customers/c1/projects/p1/part.stl";
        string? geometryRequestJson = null;
        var metadataReads = 0;

        var uploadClient = new UploadServiceClient(new HttpClient(new MockHttpMessageHandler((req, _) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains($"/files/{UploadId}"))
            {
                metadataReads++;
                var path = metadataReads == 1 ? stalePath : currentPath;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { storagePath = path })
                });
            }

            if (req.RequestUri.PathAndQuery.Contains("by-path/signed-url"))
            {
                var requestBody = req.Content!.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult();
                var requestedPath = requestBody.GetProperty("storagePath").GetString();
                if (string.Equals(requestedPath, stalePath, StringComparison.Ordinal))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Gone)
                    {
                        Content = JsonContent.Create(new { error = "file_missing" })
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { signedUrl = SignedUrl })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }))
        { BaseAddress = new Uri("http://test") });

        var geometryClient = new GeometryServiceClient(new HttpClient(new MockHttpMessageHandler(async (req, _) =>
        {
            geometryRequestJson = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new DfmAnalysisResponse
                {
                    UploadId = UploadId,
                    ProcessCode = ProcessCode,
                    Status = "analysis_complete",
                    DfmReport = new() { ReportType = ProcessCode }
                }, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                })
            };
        }))
        { BaseAddress = new Uri("http://test") });

        var controller = MakeController(uploadClient, geometryClient);

        var result = await controller.AnalyzeForProcess(
            UploadId,
            ProcessCode,
            new GeometryAnalysisRequest { StoragePath = stalePath },
            default);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DfmAnalysisResponse>(okResult.Value);
        Assert.Equal("analysis_complete", body.Status);
        Assert.NotNull(geometryRequestJson);

        using var document = JsonDocument.Parse(geometryRequestJson);
        Assert.Equal(currentPath, document.RootElement.GetProperty("storage_path").GetString());
        Assert.Equal(SignedUrl, document.RootElement.GetProperty("download_url").GetString());
    }

    [Fact]
    public async Task GetRuntimeManifest_ProxiesGeometryServiceManifestWithNoCache()
    {
        string? requestedPath = null;
        var geometryClient = MakeGeometryClient((req, _) =>
        {
            requestedPath = req.RequestUri!.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"runtimeVersion\":\"0.1.0\",\"assets\":{\"worker\":\"/geometry/client-runtime/assets/client-geometry-runtime.abc.worker.js\"}}",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
            {
                NoCache = true
            };
            return Task.FromResult(response);
        });
        var controller = MakeController(MakeUploadClient(), geometryClient);

        var result = await controller.GetRuntimeManifest(default);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Equal("application/json; charset=utf-8", content.ContentType);
        Assert.Contains("\"runtimeVersion\":\"0.1.0\"", content.Content, StringComparison.Ordinal);
        Assert.Equal("/geometry/client-runtime/manifest.json", requestedPath);
        Assert.Equal("no-cache", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task GetRuntimeAsset_ProxiesHashNamedAssetWithImmutableCache()
    {
        string? requestedPath = null;
        var geometryClient = MakeGeometryClient((req, _) =>
        {
            requestedPath = req.RequestUri!.PathAndQuery;
            var content = new ByteArrayContent([0x00, 0x61, 0xFF, 0x7F]);
            content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/wasm");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
            response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(365),
            };
            response.Headers.CacheControl.Extensions.Add(
                new System.Net.Http.Headers.NameValueHeaderValue("immutable"));
            return Task.FromResult(response);
        });
        var controller = MakeController(MakeUploadClient(), geometryClient);

        var result = await controller.GetRuntimeAsset("client-geometry-runtime.abc.worker.js", default);

        var content = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/wasm", content.ContentType);
        Assert.Equal([0x00, 0x61, 0xFF, 0x7F], content.FileContents);
        Assert.Equal("/geometry/client-runtime/assets/client-geometry-runtime.abc.worker.js", requestedPath);
        Assert.Contains("immutable", controller.Response.Headers.CacheControl.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetRuntimeAsset_Returns404_WhenGeometryServiceReturnsMissingAsset()
    {
        var geometryClient = MakeGeometryClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"detail\":\"Runtime asset not found\"}", System.Text.Encoding.UTF8, "application/json")
            }));
        var controller = MakeController(MakeUploadClient(), geometryClient);

        var result = await controller.GetRuntimeAsset("missing.worker.js", default);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(404, content.StatusCode);
        Assert.Contains("Runtime asset not found", content.Content, StringComparison.Ordinal);
    }
}
