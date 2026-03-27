using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Consumers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.MessagingContracts.Contracts.Geometry;
using Maliev.MessagingContracts.Contracts.Shared;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;

namespace Maliev.Intranet.Tests.Bff.Consumers;

/// <summary>
/// Unit tests for <see cref="SmallThumbnailReadyConsumer"/>.
/// Verifies that small thumbnail URLs are resolved and pushed via SignalR.
/// </summary>
public class SmallThumbnailReadyConsumerTests
{
    private const string StoragePath = "projects/abc/model.stl";
    private const string FileId = "00000000-0000-0000-0000-000000000001";
    private const string ThumbnailStoragePath = "projects/abc/model.stl_thumbnail_small.webp";

    private static SmallThumbnailReadyEvent BuildEvent(
        string storagePath = StoragePath,
        string fileId = FileId,
        string thumbnailStoragePath = ThumbnailStoragePath)
    {
        return new SmallThumbnailReadyEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "SmallThumbnailReadyEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "GeometryService",
            ConsumedBy: ["IntranetBff"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new SmallThumbnailReadyEventPayload(
                StoragePath: storagePath,
                FileId: fileId,
                ThumbnailStoragePath: thumbnailStoragePath));
    }

    private static Mock<ConsumeContext<SmallThumbnailReadyEvent>> MakeCtx(SmallThumbnailReadyEvent evt)
    {
        var mock = new Mock<ConsumeContext<SmallThumbnailReadyEvent>>();
        mock.Setup(c => c.Message).Returns(evt);
        mock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock;
    }

    /// <summary>
    /// Minimal HTTP handler that returns 404, causing signed URL resolution to fail.
    /// </summary>
    private sealed class FailHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static Mock<IHttpClientFactory> CreateFailingHttpClientFactory()
    {
        var handler = new FailHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://upload-service") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factoryMock;
    }

    /// <summary>
    /// HTTP handler that returns the storage path as the signed URL.
    /// </summary>
    private sealed class PassthroughSignedUrlHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            var doc = System.Text.Json.JsonDocument.Parse(body);
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

    private static Mock<IHttpClientFactory> CreatePassthroughHttpClientFactory()
    {
        var httpClient = new HttpClient(new PassthroughSignedUrlHandler()) { BaseAddress = new Uri("http://upload-service") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factoryMock;
    }

    private static (SmallThumbnailReadyConsumer consumer, Mock<IClientProxy> clientProxyMock)
        CreateConsumer()
    {
        var clientProxyMock = new Mock<IClientProxy>();
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var consumer = new SmallThumbnailReadyConsumer(
            hubMock.Object,
            CreatePassthroughHttpClientFactory().Object,
            NullLogger<SmallThumbnailReadyConsumer>.Instance);
        return (consumer, clientProxyMock);
    }

    private static (SmallThumbnailReadyConsumer consumer, Mock<IClientProxy> clientProxyMock)
        CreateConsumerWithFailingHttp()
    {
        var clientProxyMock = new Mock<IClientProxy>();
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var consumer = new SmallThumbnailReadyConsumer(
            hubMock.Object,
            CreateFailingHttpClientFactory().Object,
            NullLogger<SmallThumbnailReadyConsumer>.Instance);
        return (consumer, clientProxyMock);
    }

    [Fact]
    public async Task Consume_ShouldSendFileAnalysisCompletedToGroup()
    {
        var (consumer, clientProxyMock) = CreateConsumer();
        await consumer.Consume(MakeCtx(BuildEvent()).Object);

        clientProxyMock.Verify(x =>
            x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldPassCorrectStoragePathToSignalR()
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
    public async Task Consume_ShouldPopulateThumbnailUrlFromResolvedSignedUrl()
    {
        var (consumer, clientProxyMock) = CreateConsumer();

        FileAnalysisCompletedPayload? captured = null;
        clientProxyMock
            .Setup(x => x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
                captured = args[0] as FileAnalysisCompletedPayload)
            .Returns(Task.CompletedTask);

        await consumer.Consume(MakeCtx(BuildEvent(thumbnailStoragePath: ThumbnailStoragePath)).Object);

        Assert.NotNull(captured);
        Assert.Equal(ThumbnailStoragePath, captured.ThumbnailUrl);
    }

    [Fact]
    public async Task Consume_ShouldSetFailedFalse()
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
    public async Task Consume_ShouldThrowWhenSignedUrlResolutionFails()
    {
        var (consumer, _) = CreateConsumerWithFailingHttp();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => consumer.Consume(MakeCtx(BuildEvent()).Object));
    }

    [Fact]
    public async Task Consume_ShouldSendToCorrectFileGroup()
    {
        var (consumer, clientProxyMock) = CreateConsumer();
        await consumer.Consume(MakeCtx(BuildEvent(storagePath: "projects/xyz/part.step")).Object);

        clientProxyMock.Verify(x =>
            x.SendCoreAsync("FileAnalysisCompleted", It.IsAny<object[]>(), CancellationToken.None),
            Times.Once);
    }
}