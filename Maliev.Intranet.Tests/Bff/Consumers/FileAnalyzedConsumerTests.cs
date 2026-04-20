using Maliev.Intranet.Bff.Consumers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.MessagingContracts.Contracts.Geometry;
using Maliev.MessagingContracts.Contracts.Shared;
using Maliev.Intranet.Tests.Testing;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Consumers;

/// <summary>
/// Unit tests for <see cref="FileAnalyzedConsumer"/>.
/// Verifies that analysis completion is recorded in the status service.
/// </summary>
public class FileAnalyzedConsumerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FileAnalyzedEvent BuildEvent(
        string fileId = "file-123",
        string storagePath = "projects/abc/model.stl",
        string? glbPath = "projects/abc/model.stl_viewer.glb",
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
                    BoundingBox: new FileAnalyzedEventPayloadMetricsBoundingBox(10, 20, 30),
                    IsManifold: true,
                    TriangleCount: 1024,
                    EulerNumber: 2),
                GlbStoragePath: glbPath,
                ThumbnailStoragePath: thumbnailPath,
                StoragePath: storagePath,
                ProcessedAt: DateTimeOffset.UtcNow,
                DfmReport: null!,
                MaterialId: Guid.NewGuid(),
                MaterialCode: "PLA",
                ManufacturingProcessId: Guid.NewGuid(),
                ManufacturingProcessName: "FDM",
                BodyCount: null,
                Bodies: Array.Empty<FileAnalyzedEventPayloadBodiesItem>()));
    }

    private static Mock<ConsumeContext<FileAnalyzedEvent>> MakeCtx(FileAnalyzedEvent evt)
    {
        var mock = new Mock<ConsumeContext<FileAnalyzedEvent>>();
        mock.Setup(c => c.Message).Returns(evt);
        mock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock;
    }

    private static (FileAnalyzedConsumer consumer, Mock<IFileAnalysisStatusService> statusMock)
        CreateConsumer()
    {
        var hubMock = new Mock<IHubContext<NotificationHub>>();
        var clientsMock = new Mock<IClientProxy>();
        var hubClientsMock = new Mock<IHubClients>();
        hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientsMock.Object);
        hubMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var mockHandler = new MockHttpMessageHandler((_, _) =>
        {
            var json = """{"signedUrl": "http://storage.test/model.glb"}""";
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content });
        });
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://test/") };
        httpClientFactoryMock.Setup(f => f.CreateClient("UploadServiceClient.Consumer")).Returns(httpClient);
        var statusMock = new Mock<IFileAnalysisStatusService>();
        var consumer = new FileAnalyzedConsumer(
            hubMock.Object,
            httpClientFactoryMock.Object,
            statusMock.Object,
            NullLogger<FileAnalyzedConsumer>.Instance);
        return (consumer, statusMock);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Consume_ShouldCallSetAnalysisCompleted_WhenStoragePathPresent()
    {
        var (consumer, statusMock) = CreateConsumer();
        await consumer.Consume(MakeCtx(BuildEvent(storagePath: "projects/abc/model.stl")).Object);

        statusMock.Verify(s =>
            s.SetAnalysisCompletedAsync(
                "projects/abc/model.stl",
                "projects/abc/model.stl_viewer.glb",
                "http://storage.test/model.glb",
                null,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldNotCallSetAnalysisCompleted_WhenStoragePathMissing()
    {
        var (consumer, statusMock) = CreateConsumer();
        var evt = BuildEvent(storagePath: "");
        await consumer.Consume(MakeCtx(evt).Object);

        statusMock.Verify(s =>
            s.SetAnalysisCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_ShouldNotThrow_WhenPayloadIsNull()
    {
        var (consumer, _) = CreateConsumer();
        var mock = new Mock<ConsumeContext<FileAnalyzedEvent>>();
        mock.Setup(c => c.Message).Returns(new FileAnalyzedEvent());
        mock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(mock.Object);
        // Should complete without throwing
    }
}
