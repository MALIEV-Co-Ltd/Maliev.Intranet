using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Consumers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.MessagingContracts.Contracts.Geometry;
using Maliev.MessagingContracts.Contracts.Shared;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;

namespace Maliev.Intranet.Tests.Bff.Consumers;

/// <summary>
/// Unit tests for <see cref="FileAnalyzedConsumer"/>.
/// Verifies that geometry analysis results are pushed to SignalR clients
/// with the correct event name and payload.
/// </summary>
public class FileAnalyzedConsumerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FileAnalyzedEvent BuildEvent(
        string fileId = "file-123",
        double x = 10.0, double y = 20.0, double z = 30.0,
        string? thumbnailPath = "projects/abc/model.stl_thumb.png")
    {
        return new FileAnalyzedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "FileAnalyzedEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "GeometryService",
            ConsumedBy: ["IntranetBff"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new FileAnalyzedEventPayload(
                FileId: fileId,
                CustomerId: Guid.NewGuid(),
                Metrics: new FileAnalyzedEventPayloadMetrics(
                    VolumeCm3: 100.0,
                    SupportVolumeCm3: 5.0,
                    SurfaceAreaCm2: 200.0,
                    BoundingBox: new FileAnalyzedEventPayloadMetricsBoundingBox(x, y, z),
                    IsManifold: true,
                    TriangleCount: 1024,
                    EulerNumber: 2),
                GlbStoragePath: "projects/abc/model.stl_viewer.glb",
                ThumbnailStoragePath: thumbnailPath,
                ProcessedAt: DateTimeOffset.UtcNow,
                DfmReport: new FileAnalyzedEventPayloadDfmReport(0, [], 0, 0.0),
                MaterialId: Guid.NewGuid(),
                MaterialCode: "PLA",
                ManufacturingProcessId: Guid.NewGuid(),
                ManufacturingProcessName: "FDM"));
    }

    private static Mock<ConsumeContext<FileAnalyzedEvent>> MakeCtx(FileAnalyzedEvent evt)
    {
        var mock = new Mock<ConsumeContext<FileAnalyzedEvent>>();
        mock.Setup(c => c.Message).Returns(evt);
        mock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock;
    }

    /// <summary>
    /// Returns a mock <see cref="IHttpClientFactory"/> that produces an <see cref="HttpClient"/>
    /// backed by a fake handler returning 404 for every request, causing the consumer to fall back
    /// to the raw storage path when resolving thumbnail URLs.
    /// </summary>
    private static Mock<IHttpClientFactory> CreateNullHttpClientFactory()
    {
        var handler   = new FakeHttpMessageHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://upload-service") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factoryMock;
    }

    private static (FileAnalyzedConsumer consumer, Mock<IClientProxy> clientProxyMock)
        CreateConsumer()
    {
        var clientProxyMock = new Mock<IClientProxy>();
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);

        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var analysisStatusMock = new Mock<IFileAnalysisStatusService>();
        var consumer = new FileAnalyzedConsumer(
            hubMock.Object,
            analysisStatusMock.Object,
            NullLogger<FileAnalyzedConsumer>.Instance);
        return (consumer, clientProxyMock);
    }

    /// <summary>Minimal HTTP handler that returns a fixed status code for every request.</summary>
    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode));
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Consume_ShouldSendFileAnalysisCompletedToAllClients()
    {
        var (consumer, clientProxyMock) = CreateConsumer();
        await consumer.Consume(MakeCtx(BuildEvent()).Object);

        clientProxyMock.Verify(x =>
            x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldMapBoundingBoxToDimensions()
    {
        var (consumer, clientProxyMock) = CreateConsumer();
        var evt = BuildEvent(x: 15.5, y: 25.0, z: 35.0);

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(evt).Object);

        Assert.NotNull(captured);
        Assert.NotNull(captured.Dimensions);
        Assert.Equal(15.5, captured.Dimensions.X);
        Assert.Equal(25.0, captured.Dimensions.Y);
        Assert.Equal(35.0, captured.Dimensions.Z);
    }

    [Fact]
    public async Task Consume_ShouldSetThumbnailUrlToNull()
    {
        const string thumbPath = "projects/abc/model_thumb.png";
        var (consumer, clientProxyMock) = CreateConsumer();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(thumbnailPath: thumbPath)).Object);

        Assert.NotNull(captured);
        Assert.Null(captured.ThumbnailUrl);
    }

    [Fact]
    public async Task Consume_ShouldSetStoragePathFromGcsPathAndFileIdAsUploadId()
    {
        const string fileId = "file-abc-999";
        var (consumer, clientProxyMock) = CreateConsumer();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(fileId: fileId)).Object);

        Assert.NotNull(captured);
        Assert.Equal("projects/abc/model.stl", captured.StoragePath);
        Assert.Equal(fileId, captured.UploadId);
    }

    [Fact]
    public async Task Consume_ShouldNotSetFailed_OnSuccess()
    {
        var (consumer, clientProxyMock) = CreateConsumer();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent()).Object);

        Assert.NotNull(captured);
        Assert.False(captured.Failed);
        Assert.Null(captured.ErrorCode);
    }

    [Fact]
    public async Task Consume_WhenNoThumbnail_ShouldLeaveUrlNull()
    {
        var (consumer, clientProxyMock) = CreateConsumer();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(thumbnailPath: null)).Object);

        Assert.NotNull(captured);
        Assert.Null(captured.ThumbnailUrl);
    }
}
