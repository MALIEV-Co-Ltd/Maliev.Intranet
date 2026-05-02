using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>
/// Tests the ProjectNew resumable-upload BFF endpoints.
/// </summary>
public class UploadsControllerResumableTests
{
    private readonly Mock<IFileAnalysisStatusService> _statusServiceMock = new();
    private readonly Mock<ILogger<UploadsController>> _loggerMock = new();

    [Fact]
    public async Task InitiateResumableUploadAsync_BuildsProjectStoragePath_AndInitiatesUploadSession()
    {
        HttpRequestMessage? downstreamRequest = null;
        var uploadClient = MakeUploadClient((request, _) =>
        {
            downstreamRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    uploadId = "upload-123",
                    sessionUri = "https://storage.googleapis.com/upload/session",
                    expiresAt = DateTime.UtcNow.AddHours(1),
                    totalSize = 1024L
                })
            });
        });

        var projectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var controller = CreateController(uploadClient);
        var request = new BffInitiateResumableUploadRequest
        {
            FileName = "part.stl",
            ContentType = "model/stl",
            FileSize = 1024,
            ProjectId = projectId
        };

        var result = await controller.InitiateResumableUploadAsync(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var session = Assert.IsType<BffResumableUploadSessionResponse>(ok.Value);
        Assert.Equal("upload-123", session.UploadId);
        Assert.Equal("https://storage.googleapis.com/upload/session", session.SessionUri);
        Assert.Equal("part.stl", session.FileName);
        Assert.Equal(1024, session.FileSize);
        Assert.StartsWith($"projects/{projectId}/", session.StoragePath, StringComparison.Ordinal);
        Assert.EndsWith("_part.stl", session.StoragePath, StringComparison.Ordinal);

        Assert.NotNull(downstreamRequest);
        Assert.Equal(HttpMethod.Post, downstreamRequest.Method);
        Assert.Equal("/upload/v1/uploads/resumable", downstreamRequest.RequestUri?.AbsolutePath);
        var payload = JsonDocument.Parse(await downstreamRequest.Content!.ReadAsStringAsync()).RootElement;
        Assert.Equal(session.StoragePath, payload.GetProperty("path").GetString());
        Assert.Equal("part.stl", payload.GetProperty("fileName").GetString());
        Assert.Equal("model/stl", payload.GetProperty("contentType").GetString());
        Assert.Equal(1024, payload.GetProperty("totalSize").GetInt64());
        Assert.True(payload.GetProperty("overwrite").GetBoolean());
    }

    [Fact]
    public async Task CompleteResumableUploadAsync_CompletesUploadSession()
    {
        HttpRequestMessage? downstreamRequest = null;
        var uploadClient = MakeUploadClient((request, _) =>
        {
            downstreamRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new BffUploadResponse
                {
                    UploadId = "upload-123",
                    FileName = "part.stl",
                    FileSize = 1024,
                    StoragePath = "projects/p/part.stl"
                })
            });
        });

        var controller = CreateController(uploadClient);

        var result = await controller.CompleteResumableUploadAsync("upload-123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BffUploadResponse>(ok.Value);
        Assert.Equal("upload-123", response.UploadId);
        Assert.Equal("projects/p/part.stl", response.StoragePath);
        Assert.NotNull(downstreamRequest);
        Assert.Equal(HttpMethod.Post, downstreamRequest.Method);
        Assert.Equal("/upload/v1/uploads/resumable/upload-123/complete", downstreamRequest.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task ProxyResumableUploadAsync_ForwardsRawRequestBodyAndContentRange()
    {
        HttpRequestMessage? downstreamRequest = null;
        string? downstreamBody = null;
        var uploadClient = MakeUploadClient(async (request, _) =>
        {
            downstreamRequest = request;
            downstreamBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { uploadId = "upload-123", isComplete = true })
            };
        });

        var controller = CreateController(uploadClient);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var body = Encoding.UTF8.GetBytes("raw-file-bytes");
        controller.Request.Body = new MemoryStream(body);
        controller.Request.ContentLength = body.Length;
        controller.Request.ContentType = "model/stl";
        controller.Request.Headers.ContentRange = $"bytes 0-{body.Length - 1}/{body.Length}";

        var result = await controller.ProxyResumableUploadAsync("upload-123", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
        Assert.NotNull(downstreamRequest);
        Assert.Equal(HttpMethod.Put, downstreamRequest.Method);
        Assert.Equal("/upload/v1/uploads/resumable/upload-123", downstreamRequest.RequestUri?.AbsolutePath);
        Assert.Equal($"bytes 0-{body.Length - 1}/{body.Length}", downstreamRequest.Content?.Headers.ContentRange?.ToString());
        Assert.Equal("raw-file-bytes", downstreamBody);
    }

    [Fact]
    public void ResumableEndpoints_RequireProjectWritePermission()
    {
        var endpointNames = new[]
        {
            nameof(UploadsController.InitiateResumableUploadAsync),
            nameof(UploadsController.CompleteResumableUploadAsync),
            nameof(UploadsController.ProxyResumableUploadAsync)
        };

        foreach (var endpointName in endpointNames)
        {
            var method = typeof(UploadsController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(m => m.Name == endpointName);

            var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());
            Assert.Contains(MalievPermissions.Project.Write, attribute.Policy, StringComparison.Ordinal);
            Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
        }
    }

    private UploadsController CreateController(UploadServiceClient uploadClient)
    {
        return new UploadsController(uploadClient, _statusServiceMock.Object, CreateFileTypes(), _loggerMock.Object);
    }

    private static UploadServiceClient MakeUploadClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        return new UploadServiceClient(new HttpClient(new MockHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://upload-service")
        });
    }

    private static FileTypesSettings CreateFileTypes()
    {
        return new FileTypesSettings
        {
            ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".stl", ".step", ".stp", ".3mf" },
            DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            DrawingExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            OfficeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            SupplementaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };
    }
}
