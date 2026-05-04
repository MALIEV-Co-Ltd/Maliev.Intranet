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
using Microsoft.AspNetCore.WebUtilities;
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

    [Fact]
    public async Task MigrateProjectAsync_WhenTempAnalysisAlreadyCompleted_ReturnsReconciledStatusForNewPath()
    {
        const string OldPath = "projects/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/part.stl";
        const string NewPath = "customers/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/projects/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/part.stl";
        const string NewGlbPath = NewPath + "_viewer.glb";

        var fileId = Guid.NewGuid().ToString();
        var projectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var customerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        HttpRequestMessage? migrationRequest = null;
        var copiedPaths = new List<(string Source, string Destination)>();

        var uploadClient = MakeUploadClient((request, _) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/upload/v1/admin/migrate-project/" + projectId, StringComparison.Ordinal))
            {
                migrationRequest = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        dryRun = false,
                        totalEvaluated = 1,
                        totalMigrated = 1,
                        migratedFiles = new[]
                        {
                            new { fileId, oldPath = OldPath, newPath = NewPath }
                        },
                        errors = Array.Empty<string>()
                    })
                });
            }

            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/upload/v1/admin/copy-file", StringComparison.Ordinal))
            {
                var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
                copiedPaths.Add((query["sourcePath"].ToString(), query["destinationPath"].ToString()));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/upload/v1/files/by-path/signed-url", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { signedUrl = "https://signed.example/new-viewer.glb" })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var oldStatus = new FileAnalysisStatusDto
        {
            UploadId = OldPath,
            Status = FileAnalysisStatus.Completed,
            Dimensions = new FileAnalysisDimensionsDto { X = 10, Y = 20, Z = 30, VolumeMm3 = 6000 },
            IsManifold = true,
            ThumbnailUrl = "https://signed.example/thumb-small.webp",
            HiResThumbnailUrl = "https://signed.example/thumb-large.webp",
            PreviewUrls = new FileAnalysisPreviewUrlsDto
            {
                ThumbnailSmall = "https://signed.example/thumb-small.webp",
                ThumbnailLargeUrl = "https://signed.example/thumb-large.webp",
                ThumbnailSmallGcsPath = OldPath + "_thumb_256.webp",
                ThumbnailLargeGcsPath = OldPath + "_thumb_1200.webp"
            },
            GlbStoragePath = OldPath + "_viewer.glb",
            GlbSignedUrl = "https://signed.example/old-viewer.glb",
            PreviewProcessingStatus = PreviewProcessingStatus.Completed,
            ProcessedAt = DateTimeOffset.Parse("2026-05-04T12:00:00Z")
        };
        var clonedStatus = oldStatus with
        {
            UploadId = NewPath,
            GlbStoragePath = NewGlbPath,
            GlbSignedUrl = "https://signed.example/new-viewer.glb"
        };

        _statusServiceMock
            .Setup(service => service.RegisterStoragePathAliasAsync(OldPath, NewPath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _statusServiceMock
            .Setup(service => service.GetStatusAsync(OldPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldStatus);
        _statusServiceMock
            .Setup(service => service.CloneStatusAsync(
                OldPath,
                NewPath,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                NewGlbPath,
                "https://signed.example/new-viewer.glb",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _statusServiceMock
            .Setup(service => service.GetStatusAsync(NewPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clonedStatus);

        var controller = CreateController(uploadClient);

        var result = await controller.MigrateProjectAsync(projectId, customerId, dryRun: false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BffMigrateProjectResponseDto>(ok.Value);
        var migratedFile = Assert.Single(response.MigratedFiles);

        Assert.NotNull(migrationRequest);
        Assert.Equal(fileId, migratedFile.FileId);
        Assert.Equal(OldPath, migratedFile.OldPath);
        Assert.Equal(NewPath, migratedFile.NewPath);
        Assert.NotNull(migratedFile.Status);
        Assert.Equal(NewPath, migratedFile.Status.UploadId);
        Assert.Equal(FileAnalysisStatus.Completed, migratedFile.Status.Status);
        Assert.Equal(NewGlbPath, migratedFile.Status.GlbStoragePath);
        Assert.Equal("https://signed.example/new-viewer.glb", migratedFile.Status.GlbSignedUrl);
        Assert.Equal(PreviewProcessingStatus.Completed, migratedFile.Status.PreviewProcessingStatus);

        var statusResult = await controller.GetAnalysisStatusAsync(NewPath, CancellationToken.None);
        var statusOk = Assert.IsType<OkObjectResult>(statusResult.Result);
        var endpointStatus = Assert.IsType<FileAnalysisStatusDto>(statusOk.Value);

        Assert.Equal(NewPath, endpointStatus.UploadId);
        Assert.Equal(FileAnalysisStatus.Completed, endpointStatus.Status);
        Assert.Equal(NewGlbPath, endpointStatus.GlbStoragePath);

        Assert.Contains(copiedPaths, copy => copy.Source == OldPath + "_viewer.glb" && copy.Destination == NewGlbPath);
    }

    [Fact]
    public void UploadServiceMigrationResponseContract_DeserializesCamelCaseWireShape()
    {
        var json = """
        {
          "dryRun": false,
          "totalEvaluated": 1,
          "totalMigrated": 1,
          "migratedFiles": [
            {
              "fileId": "file-1",
              "oldPath": "projects/project-1/part.stl",
              "newPath": "customers/customer-1/projects/project-1/part.stl"
            }
          ],
          "errors": []
        }
        """;

        var response = JsonSerializer.Deserialize<MigrateProjectResponse>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(response);
        Assert.False(response.DryRun);
        Assert.Equal(1, response.TotalEvaluated);
        Assert.Equal(1, response.TotalMigrated);
        var file = Assert.Single(response.MigratedFiles);
        Assert.Equal("file-1", file.FileId);
        Assert.Equal("projects/project-1/part.stl", file.OldPath);
        Assert.Equal("customers/customer-1/projects/project-1/part.stl", file.NewPath);
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
