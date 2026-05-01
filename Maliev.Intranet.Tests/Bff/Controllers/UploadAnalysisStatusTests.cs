using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;

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

    private UploadsController CreateController()
    {
        // Create a minimal HttpClient (won't be used in GetAnalysisStatusAsync)
        var httpClient = new HttpClient();
        var uploadClient = new UploadServiceClient(httpClient);

        var fileTypesSettings = new FileTypesSettings
        {
            ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            DrawingExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            OfficeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            SupplementaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };
        return new UploadsController(uploadClient, _statusServiceMock.Object, fileTypesSettings, _loggerMock.Object);
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
