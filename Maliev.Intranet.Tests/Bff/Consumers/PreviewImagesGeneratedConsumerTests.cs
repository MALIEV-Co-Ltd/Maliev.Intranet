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
        string? front          = "projects/abc/model.stl_preview_front_small.webp",
        string? back           = "projects/abc/model.stl_preview_back_small.webp",
        string? left           = "projects/abc/model.stl_preview_left_small.webp",
        string? right          = "projects/abc/model.stl_preview_right_small.webp",
        string? top            = "projects/abc/model.stl_preview_top_small.webp",
        string? bottom         = "projects/abc/model.stl_preview_bottom_small.webp",
        string? iso            = "projects/abc/model.stl_thumbnail_small.webp",
        string? iso1000        = "projects/abc/model.stl_thumbnail_large.webp",
        bool failed            = false)
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
                    FrontSmall: front, LeftSmall: left, RightSmall: right,
                    BackSmall: back, TopSmall: top, BottomSmall: bottom,
                    ThumbnailSmall: iso, ThumbnailLarge: iso1000),
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
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

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

    /// <summary>
    /// HTTP handler that reads the storage path from the JSON request body and
    /// returns it as the <c>signedUrl</c>, simulating successful URL resolution.
    /// Handles both PascalCase and camelCase property names.
    /// </summary>
    private sealed class PassthroughSignedUrlHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            var doc  = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;
            string path = string.Empty;
            if (root.TryGetProperty("StoragePath", out var p1))
                path = p1.GetString() ?? string.Empty;
            else if (root.TryGetProperty("storagePath", out var p2))
                path = p2.GetString() ?? string.Empty;
            var json = $"{{\"signedUrl\":\"{path}\"}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    /// <summary>
    /// Returns a mock <see cref="IHttpClientFactory"/> whose HTTP client passes the storage path
    /// through as the signed URL — used for tests that verify URL routing logic.
    /// </summary>
    private static Mock<IHttpClientFactory> CreatePassthroughHttpClientFactory()
    {
        var httpClient  = new HttpClient(new PassthroughSignedUrlHandler()) { BaseAddress = new Uri("http://upload-service") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factoryMock;
    }

    private static (PreviewImagesGeneratedConsumer consumer, Mock<IClientProxy> clientProxyMock)
        CreateConsumerWithPassthrough()
    {
        var clientProxyMock = new Mock<IClientProxy>();
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var analysisStatusMock = new Mock<IFileAnalysisStatusService>();
        var consumer = new PreviewImagesGeneratedConsumer(
            hubMock.Object,
            CreatePassthroughHttpClientFactory().Object,
            analysisStatusMock.Object,
            NullLogger<PreviewImagesGeneratedConsumer>.Instance);
        return (consumer, clientProxyMock);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Consume_ShouldSendFileAnalysisCompletedToAllClients()
    {
        var (consumer, clientProxyMock) = CreateConsumerWithPassthrough();
        await consumer.Consume(MakeCtx(BuildEvent()).Object);

        clientProxyMock.Verify(x =>
            x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldMapAllSixPreviewUrls()
    {
        var (consumer, clientProxyMock) = CreateConsumerWithPassthrough();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(
            front: "p/front.webp", back: "p/back.webp",
            left: "p/left.webp", right: "p/right.webp",
            top: "p/top.webp", bottom: "p/bottom.webp")).Object);

        Assert.NotNull(captured?.PreviewUrls);
        Assert.Equal("p/front.webp",  captured.PreviewUrls.FrontSmall);
        Assert.Equal("p/back.webp",   captured.PreviewUrls.BackSmall);
        Assert.Equal("p/left.webp",   captured.PreviewUrls.LeftSmall);
        Assert.Equal("p/right.webp",  captured.PreviewUrls.RightSmall);
        Assert.Equal("p/top.webp",    captured.PreviewUrls.TopSmall);
        Assert.Equal("p/bottom.webp", captured.PreviewUrls.BottomSmall);
    }

    [Fact]
    public async Task Consume_ShouldSetStoragePath()
    {
        var (consumer, clientProxyMock) = CreateConsumerWithPassthrough();

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
        var (consumer, clientProxyMock) = CreateConsumerWithPassthrough();

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
        var (consumer, clientProxyMock) = CreateConsumerWithPassthrough();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(
            front: "p/front.webp", back: null, left: null, right: null, top: null, bottom: null)).Object);

        Assert.NotNull(captured?.PreviewUrls);
        Assert.Equal("p/front.webp", captured.PreviewUrls.FrontSmall);
        Assert.Null(captured.PreviewUrls.BackSmall);
        Assert.Null(captured.PreviewUrls.LeftSmall);
        Assert.Null(captured.PreviewUrls.RightSmall);
        Assert.Null(captured.PreviewUrls.TopSmall);
        Assert.Null(captured.PreviewUrls.BottomSmall);
    }

    [Fact]
    public async Task Consume_WhenThumbnailLargePresent_ShouldPopulateHiResThumbnailUrl()
    {
        const string thumbnailLargePath = "projects/abc/model.stl_thumbnail_large.webp";
        var (consumer, clientProxyMock) = CreateConsumerWithPassthrough();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(iso1000: thumbnailLargePath)).Object);

        Assert.NotNull(captured);
        Assert.Equal(captured.HiResThumbnailUrl, captured.PreviewUrls?.ThumbnailLarge);
    }

    [Fact]
    public async Task Consume_WhenThumbnailLargeNull_HiResThumbnailUrlShouldBeNull()
    {
        var (consumer, clientProxyMock) = CreateConsumerWithPassthrough();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(iso1000: null)).Object);

        Assert.NotNull(captured);
        Assert.Null(captured.HiResThumbnailUrl);
        Assert.Null(captured.PreviewUrls?.ThumbnailLarge);
    }
}
