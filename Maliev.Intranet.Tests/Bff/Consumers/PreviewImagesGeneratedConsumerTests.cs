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
/// Unit tests for <see cref="PreviewImagesGeneratedConsumer"/>.
/// Verifies that preview image URLs from GeometryService are forwarded
/// to all SignalR clients via the FileAnalysisCompleted event.
/// </summary>
public class PreviewImagesGeneratedConsumerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private const string StoragePath = "projects/abc/model.stl";

    private static PreviewImagesGeneratedEvent BuildEvent(
        string? front  = "projects/abc/model.stl_preview_front.png",
        string? back   = "projects/abc/model.stl_preview_back.png",
        string? left   = "projects/abc/model.stl_preview_left.png",
        string? right  = "projects/abc/model.stl_preview_right.png",
        string? top    = "projects/abc/model.stl_preview_top.png",
        string? bottom = "projects/abc/model.stl_preview_bottom.png",
        string? iso    = "projects/abc/model.stl_preview_iso_256.png",
        bool failed    = false)
    {
        return new PreviewImagesGeneratedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "PreviewImagesGeneratedEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "GeometryService",
            ConsumedBy: ["IntranetBff"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PreviewImagesGeneratedEventPayload(
                StoragePath: StoragePath,
                PreviewImages: new PreviewImagesGeneratedEventPayloadPreviewImages(
                    Front: front, Left: left, Right: right,
                    Back: back, Top: top, Bottom: bottom, Iso: iso),
                GeneratedAt: DateTimeOffset.UtcNow,
                Failed: failed));
    }

    private static Mock<ConsumeContext<PreviewImagesGeneratedEvent>> MakeCtx(PreviewImagesGeneratedEvent evt)
    {
        var mock = new Mock<ConsumeContext<PreviewImagesGeneratedEvent>>();
        mock.Setup(c => c.Message).Returns(evt);
        mock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock;
    }

    /// <summary>
    /// Returns a mock <see cref="IHttpClientFactory"/> that produces an <see cref="HttpClient"/>
    /// backed by a fake handler returning 404 for every request, causing the consumer to fall back
    /// to the raw storage path when resolving preview image URLs.
    /// </summary>
    private static Mock<IHttpClientFactory> CreateNullHttpClientFactory()
    {
        var handler    = new FakeHttpMessageHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://upload-service") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factoryMock;
    }

    private static (PreviewImagesGeneratedConsumer consumer, Mock<IClientProxy> clientProxyMock)
        CreateConsumer()
    {
        var clientProxyMock = new Mock<IClientProxy>();
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);

        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var analysisStatusMock = new Mock<IFileAnalysisStatusService>();
        var consumer = new PreviewImagesGeneratedConsumer(
            hubMock.Object,
            CreateNullHttpClientFactory().Object,
            analysisStatusMock.Object,
            NullLogger<PreviewImagesGeneratedConsumer>.Instance);
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
    public async Task Consume_ShouldMapAllSixPreviewUrls()
    {
        var (consumer, clientProxyMock) = CreateConsumer();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(
            front: "p/front.png", back: "p/back.png",
            left: "p/left.png", right: "p/right.png",
            top: "p/top.png", bottom: "p/bottom.png")).Object);

        Assert.NotNull(captured?.PreviewUrls);
        Assert.Equal("p/front.png",  captured.PreviewUrls.Front);
        Assert.Equal("p/back.png",   captured.PreviewUrls.Back);
        Assert.Equal("p/left.png",   captured.PreviewUrls.Left);
        Assert.Equal("p/right.png",  captured.PreviewUrls.Right);
        Assert.Equal("p/top.png",    captured.PreviewUrls.Top);
        Assert.Equal("p/bottom.png", captured.PreviewUrls.Bottom);
    }

    [Fact]
    public async Task Consume_ShouldPreferIsoPreviewAsThumbnailUrl()
    {
        const string isoUrl = "p/iso.png";
        var (consumer, clientProxyMock) = CreateConsumer();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(front: "p/front.png", iso: isoUrl)).Object);

        Assert.NotNull(captured);
        Assert.Equal(isoUrl, captured.ThumbnailUrl);
    }

    [Fact]
    public async Task Consume_ShouldUseFrontPreviewAsThumbnailUrl_WhenIsoNotAvailable()
    {
        const string frontUrl = "p/front.png";
        var (consumer, clientProxyMock) = CreateConsumer();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(front: frontUrl, iso: null)).Object);

        Assert.NotNull(captured);
        Assert.Equal(frontUrl, captured.ThumbnailUrl);
    }

    [Fact]
    public async Task Consume_ShouldSetStoragePath()
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
        Assert.Equal(StoragePath, captured.StoragePath);
    }

    [Fact]
    public async Task Consume_ShouldNotSetFailedOrDimensions()
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
        Assert.Null(captured.Dimensions);
        Assert.Null(captured.UploadId);
    }

    [Fact]
    public async Task Consume_WhenSomeSidesNull_ShouldPreserveNulls()
    {
        var (consumer, clientProxyMock) = CreateConsumer();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(
            front: "p/front.png", back: null, left: null, right: null, top: null, bottom: null)).Object);

        Assert.NotNull(captured?.PreviewUrls);
        Assert.Equal("p/front.png", captured.PreviewUrls.Front);
        Assert.Null(captured.PreviewUrls.Back);
        Assert.Null(captured.PreviewUrls.Left);
        Assert.Null(captured.PreviewUrls.Right);
        Assert.Null(captured.PreviewUrls.Top);
        Assert.Null(captured.PreviewUrls.Bottom);
    }
}
