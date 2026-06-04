using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>
/// Tests for the /api/v1/uploads/analysis-status endpoint.
/// Verifies that the endpoint correctly returns analysis status from
/// IFileAnalysisStatusService.
/// </summary>
public class UploadAnalysisStatusTests
{
    private readonly Mock<IFileAnalysisStatusService> _statusServiceMock = new();
    private readonly Mock<ILogger<UploadsController>> _loggerMock = new();

    private UploadsController CreateController(UploadServiceClient? uploadClient = null)
    {
        uploadClient ??= new UploadServiceClient(new HttpClient());

        var fileTypesSettings = new FileTypesSettings
        {
            ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".stl",
                ".step",
                ".stp",
                ".3mf",
                ".obj",
                ".igs",
                ".iges",
                ".blend",
                ".fbx",
                ".gltf",
                ".glb"
            },
            DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            DrawingExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            OfficeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            SupplementaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        return new UploadsController(
            uploadClient,
            httpClientFactoryMock.Object,
            _statusServiceMock.Object,
            fileTypesSettings,
            _loggerMock.Object);
    }

    /// <summary>
    /// Tests that the endpoint returns Ok with completed status when analysis is done.
    /// </summary>
    [Fact]
    public async Task GetAnalysisStatus_ReturnsCompleted_WhenServiceReturnsStatus()
    {
        // Arrange
        var storagePath = "projects/test/model.step";
        var expectedStatus = new FileAnalysisStatusDto
        {
            UploadId = storagePath,
            Status = FileAnalysisStatus.Completed,
            GlbStoragePath = "gs://bucket/model.glb",
            GlbSignedUrl = "https://storage.googleapis.com/signed-url.glb",
            Dimensions = new FileAnalysisDimensionsDto { X = 100, Y = 200, Z = 50, VolumeMm3 = 5000 },
            IsManifold = true
        };

        _statusServiceMock
            .Setup(x => x.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        var controller = CreateController();

        // Act
        var result = await controller.GetAnalysisStatusAsync(storagePath, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStatus = Assert.IsType<FileAnalysisStatusDto>(okResult.Value);
        Assert.Equal(FileAnalysisStatus.Completed, returnedStatus.Status);
        Assert.Equal("gs://bucket/model.glb", returnedStatus.GlbStoragePath);
        Assert.Equal("https://storage.googleapis.com/signed-url.glb", returnedStatus.GlbSignedUrl);
    }

    /// <summary>
    /// Tests that the endpoint returns NotFound when status doesn't exist.
    /// </summary>
    [Fact]
    public async Task GetAnalysisStatus_ReturnsNotFound_WhenUnknown()
    {
        // Arrange
        var storagePath = "projects/unknown/model.step";

        _statusServiceMock
            .Setup(x => x.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileAnalysisStatusDto?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetAnalysisStatusAsync(storagePath, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAnalysisStatus_WhenCacheMissForSourceStl_RecoversConventionalGlbArtifact()
    {
        var storagePath = "projects/test/model.stl";
        var glbStoragePath = "projects/test/model.stl_viewer.glb";
        var signedUrl = "https://storage.example/model.stl_viewer.glb?signature=fresh";

        var uploadClient = new UploadServiceClient(new HttpClient(new MockHttpMessageHandler(async (request, _) =>
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
            using var requestJson = JsonDocument.Parse(body);
            var requestedPath = requestJson.RootElement.GetProperty("storagePath").GetString();

            return string.Equals(requestedPath, glbStoragePath, StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { signedUrl })
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }))
        {
            BaseAddress = new Uri("http://upload-service")
        });

        _statusServiceMock
            .Setup(x => x.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileAnalysisStatusDto?)null);

        var controller = CreateController(uploadClient);

        var result = await controller.GetAnalysisStatusAsync(storagePath, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FileAnalysisStatusDto>(okResult.Value);
        Assert.Equal(storagePath, status.UploadId);
        Assert.Equal(FileAnalysisStatus.Completed, status.Status);
        Assert.Equal(glbStoragePath, status.GlbStoragePath);
        Assert.Equal(glbStoragePath, status.ViewerStoragePath);
        Assert.Equal(".glb", status.ViewerFileExtension);
        Assert.Equal(signedUrl, status.GlbSignedUrl);
        Assert.Equal(PreviewProcessingStatus.Completed, status.PreviewProcessingStatus);
    }

    [Fact]
    public async Task GetAnalysisStatus_WhenCacheMissForBrowserViewerSource_SignsOriginalPath()
    {
        var storagePath = "projects/test/model.stl";
        var signedUrl = "https://storage.example/model.stl?signature=fresh";
        var signedUrlRequestBodies = new List<string>();

        var uploadClient = new UploadServiceClient(new HttpClient(new MockHttpMessageHandler(async (request, _) =>
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
            lock (signedUrlRequestBodies) { signedUrlRequestBodies.Add(body); }
            using var requestJson = JsonDocument.Parse(body);
            var requestedPath = requestJson.RootElement.GetProperty("storagePath").GetString();

            return string.Equals(requestedPath, storagePath, StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { signedUrl })
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }))
        {
            BaseAddress = new Uri("http://upload-service")
        });

        _statusServiceMock
            .Setup(x => x.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileAnalysisStatusDto?)null);

        var controller = CreateController(uploadClient);

        var result = await controller.GetAnalysisStatusAsync(storagePath, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FileAnalysisStatusDto>(okResult.Value);
        Assert.Equal(storagePath, status.UploadId);
        Assert.Equal(FileAnalysisStatus.Completed, status.Status);
        Assert.Null(status.GlbStoragePath);
        Assert.Equal(storagePath, status.ViewerStoragePath);
        Assert.Equal(".stl", status.ViewerFileExtension);
        Assert.Equal(signedUrl, status.GlbSignedUrl);
        Assert.Equal(PreviewProcessingStatus.Completed, status.PreviewProcessingStatus);
        var signedUrlRequestBody = Assert.Single(signedUrlRequestBodies);
        using var signedUrlRequestJson = JsonDocument.Parse(signedUrlRequestBody);
        Assert.Equal(storagePath, signedUrlRequestJson.RootElement.GetProperty("storagePath").GetString());
    }

    /// <summary>
    /// Tests that the endpoint returns processing status correctly.
    /// </summary>
    [Fact]
    public async Task GetAnalysisStatus_ReturnsProcessing_WhenAnalysisInProgress()
    {
        // Arrange
        var storagePath = "projects/processing/model.step";
        var processingStatus = new FileAnalysisStatusDto
        {
            UploadId = storagePath,
            Status = FileAnalysisStatus.Processing,
            GlbStoragePath = null, // No GLB yet
            GlbSignedUrl = null
        };

        _statusServiceMock
            .Setup(x => x.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(processingStatus);

        var controller = CreateController();

        // Act
        var result = await controller.GetAnalysisStatusAsync(storagePath, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStatus = Assert.IsType<FileAnalysisStatusDto>(okResult.Value);
        Assert.Equal(FileAnalysisStatus.Processing, returnedStatus.Status);
        Assert.Null(returnedStatus.GlbStoragePath);
    }

    /// <summary>
    /// Tests that the viewer URL endpoint resolves status by the original source path,
    /// then signs the generated GLB artifact path.
    /// </summary>
    [Fact]
    public async Task GetViewerUrl_UsesSourceStoragePathStatus_AndSignsGlbArtifact()
    {
        var storagePath = "projects/test/model.step";
        var glbStoragePath = "projects/test/model.step_viewer.glb";
        var signedUrl = "https://storage.example/model.glb?signature=fresh";
        var signedUrlRequests = new List<HttpRequestMessage>();
        var signedUrlRequestBodies = new List<string>();

        var uploadClient = new UploadServiceClient(new HttpClient(new MockHttpMessageHandler(async (request, _) =>
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
            lock (signedUrlRequests) { signedUrlRequests.Add(request); }
            lock (signedUrlRequestBodies) { signedUrlRequestBodies.Add(body); }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl })
            };
        }))
        {
            BaseAddress = new Uri("http://upload-service")
        });

        _statusServiceMock
            .Setup(x => x.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileAnalysisStatusDto
            {
                UploadId = storagePath,
                Status = FileAnalysisStatus.Completed,
                GlbStoragePath = glbStoragePath
            });

        var controller = CreateController(uploadClient);

        var result = await controller.GetViewerUrlAsync(storagePath, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(signedUrl, document.RootElement.GetProperty("Url").GetString());
        Assert.Equal(glbStoragePath, document.RootElement.GetProperty("ViewerStoragePath").GetString());
        Assert.Equal(".glb", document.RootElement.GetProperty("ViewerFileExtension").GetString());
        Assert.Contains(signedUrlRequests, request =>
            request.RequestUri?.AbsolutePath == "/upload/v1/files/by-path/signed-url");
        var signedUrlRequestBody = Assert.Single(signedUrlRequestBodies);
        using var signedUrlRequestJson = JsonDocument.Parse(signedUrlRequestBody);
        Assert.True(signedUrlRequestJson.RootElement.TryGetProperty("storagePath", out var signedStoragePath));
        Assert.Equal(glbStoragePath, signedStoragePath.GetString());
    }

    [Fact]
    public async Task GetViewerUrl_WhenViewerSourceIsOriginalStl_SignsOriginalPathAndReturnsStlExtension()
    {
        var storagePath = "projects/test/model.stl";
        var signedUrl = "https://storage.example/model.stl?signature=fresh";
        var signedUrlRequestBodies = new List<string>();

        var uploadClient = new UploadServiceClient(new HttpClient(new MockHttpMessageHandler(async (request, _) =>
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
            lock (signedUrlRequestBodies) { signedUrlRequestBodies.Add(body); }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl })
            };
        }))
        {
            BaseAddress = new Uri("http://upload-service")
        });

        _statusServiceMock
            .Setup(x => x.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileAnalysisStatusDto
            {
                UploadId = storagePath,
                Status = FileAnalysisStatus.Completed,
                ViewerStoragePath = storagePath,
                ViewerFileExtension = ".stl",
                GlbSignedUrl = signedUrl
            });

        var controller = CreateController(uploadClient);

        var result = await controller.GetViewerUrlAsync(storagePath, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(signedUrl, document.RootElement.GetProperty("Url").GetString());
        Assert.Equal(storagePath, document.RootElement.GetProperty("ViewerStoragePath").GetString());
        Assert.Equal(".stl", document.RootElement.GetProperty("ViewerFileExtension").GetString());
        var signedUrlRequestBody = Assert.Single(signedUrlRequestBodies);
        using var signedUrlRequestJson = JsonDocument.Parse(signedUrlRequestBody);
        Assert.Equal(storagePath, signedUrlRequestJson.RootElement.GetProperty("storagePath").GetString());
    }

    [Fact]
    public async Task GetViewerUrl_WhenCacheMissForSourceStl_FallsBackToConventionalGlbArtifact()
    {
        var storagePath = "projects/test/model.stl";
        var glbStoragePath = "projects/test/model.stl_viewer.glb";
        var signedUrl = "https://storage.example/model.stl_viewer.glb?signature=fresh";
        var signedUrlRequestBodies = new List<string>();

        var uploadClient = new UploadServiceClient(new HttpClient(new MockHttpMessageHandler(async (request, _) =>
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
            lock (signedUrlRequestBodies) { signedUrlRequestBodies.Add(body); }

            using var requestJson = JsonDocument.Parse(body);
            var requestedPath = requestJson.RootElement.GetProperty("storagePath").GetString();

            return string.Equals(requestedPath, glbStoragePath, StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { signedUrl })
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }))
        {
            BaseAddress = new Uri("http://upload-service")
        });

        _statusServiceMock
            .Setup(x => x.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileAnalysisStatusDto?)null);

        var controller = CreateController(uploadClient);

        var result = await controller.GetViewerUrlAsync(storagePath, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(signedUrl, document.RootElement.GetProperty("Url").GetString());
        Assert.Equal(glbStoragePath, document.RootElement.GetProperty("ViewerStoragePath").GetString());
        Assert.Equal(".glb", document.RootElement.GetProperty("ViewerFileExtension").GetString());
        Assert.Equal(2, signedUrlRequestBodies.Count);
        using var originalSignedUrlRequestJson = JsonDocument.Parse(signedUrlRequestBodies[0]);
        Assert.Equal(storagePath, originalSignedUrlRequestJson.RootElement.GetProperty("storagePath").GetString());
        using var glbSignedUrlRequestJson = JsonDocument.Parse(signedUrlRequestBodies[1]);
        Assert.Equal(glbStoragePath, glbSignedUrlRequestJson.RootElement.GetProperty("storagePath").GetString());
    }

    [Fact]
    public async Task GetViewerUrl_WhenCacheMissForBrowserViewerSource_SignsOriginalPath()
    {
        var storagePath = "projects/test/model.stl";
        var signedUrl = "https://storage.example/model.stl?signature=fresh";
        var signedUrlRequestBodies = new List<string>();

        var uploadClient = new UploadServiceClient(new HttpClient(new MockHttpMessageHandler(async (request, _) =>
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
            lock (signedUrlRequestBodies) { signedUrlRequestBodies.Add(body); }
            using var requestJson = JsonDocument.Parse(body);
            var requestedPath = requestJson.RootElement.GetProperty("storagePath").GetString();

            return string.Equals(requestedPath, storagePath, StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { signedUrl })
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }))
        {
            BaseAddress = new Uri("http://upload-service")
        });

        _statusServiceMock
            .Setup(x => x.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileAnalysisStatusDto?)null);

        var controller = CreateController(uploadClient);

        var result = await controller.GetViewerUrlAsync(storagePath, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(signedUrl, document.RootElement.GetProperty("Url").GetString());
        Assert.Equal(storagePath, document.RootElement.GetProperty("ViewerStoragePath").GetString());
        Assert.Equal(".stl", document.RootElement.GetProperty("ViewerFileExtension").GetString());
        var signedUrlRequestBody = Assert.Single(signedUrlRequestBodies);
        using var signedUrlRequestJson = JsonDocument.Parse(signedUrlRequestBody);
        Assert.Equal(storagePath, signedUrlRequestJson.RootElement.GetProperty("storagePath").GetString());
    }

    /// <summary>
    /// Tests that the endpoint correctly returns dimensions and manifold status.
    /// </summary>
    [Fact]
    public async Task GetAnalysisStatus_ReturnsDimensions_WhenAnalysisComplete()
    {
        // Arrange
        var storagePath = "projects/dimensions/part.step";
        var statusWithDimensions = new FileAnalysisStatusDto
        {
            UploadId = storagePath,
            Status = FileAnalysisStatus.Completed,
            Dimensions = new FileAnalysisDimensionsDto
            {
                X = 150.5,
                Y = 200.0,
                Z = 75.25,
                VolumeMm3 = 12500.50
            },
            IsManifold = true
        };

        _statusServiceMock
            .Setup(x => x.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statusWithDimensions);

        var controller = CreateController();

        // Act
        var result = await controller.GetAnalysisStatusAsync(storagePath, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStatus = Assert.IsType<FileAnalysisStatusDto>(okResult.Value);
        Assert.NotNull(returnedStatus.Dimensions);
        Assert.Equal(150.5, returnedStatus.Dimensions.X);
        Assert.Equal(200.0, returnedStatus.Dimensions.Y);
        Assert.Equal(75.25, returnedStatus.Dimensions.Z);
        Assert.Equal(12500.50, returnedStatus.Dimensions.VolumeMm3);
        Assert.True(returnedStatus.IsManifold);
    }
}
