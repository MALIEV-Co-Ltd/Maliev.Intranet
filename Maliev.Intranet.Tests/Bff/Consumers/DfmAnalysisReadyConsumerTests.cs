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
/// Unit tests for <see cref="DfmAnalysisReadyConsumer"/>.
/// Verifies DFM report caching, overlay URL signing, and SignalR broadcast.
/// </summary>
public class DfmAnalysisReadyConsumerTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private const string StoragePath = "projects/abc/model.stl";
    private const string FileId      = "file-001";

    private static DfmAnalysisReadyEvent BuildEvent(
        string?                         storagePath  = StoragePath,
        Dictionary<string, string>?     overlayPaths = null,
        bool                            nullPayload  = false)
    {
        var fdmReport = new FdmDfmReportPayload(
            ReportType:               "FDM",
            ThinWallCount:            2,
            ThinWallRegions:          [],
            OverhangFaceCount:        5,
            OverhangAreaCm2:          12.0,
            OverhangRegions:          [],
            SupportRequired:          true,
            EstimatedSupportVolumeCm3: 3.0,
            SmallDetailCount:         0);

        var payload = nullPayload ? null! : new DfmAnalysisReadyEventPayload(
            FileId:      FileId,
            StoragePath: storagePath ?? string.Empty,
            FdmReport:   fdmReport,
            SlaReport:   null!,
            CncReport:   null!,
            AnalyzedAt:  DateTimeOffset.UtcNow,
            OverlayPaths: overlayPaths);

        return new DfmAnalysisReadyEvent(
            MessageId:      Guid.NewGuid(),
            MessageName:    "DfmAnalysisReadyEvent",
            MessageType:    MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy:    "GeometryService",
            ConsumedBy:     ["IntranetBff"],
            CorrelationId:  Guid.NewGuid(),
            CausationId:    null,
            OccurredAtUtc:  DateTimeOffset.UtcNow,
            IsPublic:       false,
            Payload:        payload);
    }

    private static Mock<ConsumeContext<DfmAnalysisReadyEvent>> MakeCtx(DfmAnalysisReadyEvent evt)
    {
        var mock = new Mock<ConsumeContext<DfmAnalysisReadyEvent>>();
        mock.Setup(c => c.Message).Returns(evt);
        mock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock;
    }

    /// <summary>HTTP handler that always returns 404 — simulates failed URL signing.</summary>
    private sealed class NotFoundHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// HTTP handler that echoes the storage path from the request body back as
    /// <c>signedUrl</c> — simulates a successful UploadService URL signing call.
    /// </summary>
    private sealed class PassthroughSignedUrlHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            var doc  = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;
            var path = string.Empty;
            if (root.TryGetProperty("StoragePath", out var p1)) path = p1.GetString() ?? string.Empty;
            else if (root.TryGetProperty("storagePath", out var p2)) path = p2.GetString() ?? string.Empty;
            var json = $"{{\"signedUrl\":\"{path}\"}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static Mock<IHttpClientFactory> CreateHttpClientFactory(HttpMessageHandler handler)
    {
        var httpClient  = new HttpClient(handler) { BaseAddress = new Uri("http://upload-service") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factoryMock;
    }

    private static (DfmAnalysisReadyConsumer consumer,
                    Mock<IClientProxy>        signalrProxy,
                    Mock<IFileAnalysisStatusService> statusService)
        CreateConsumer(HttpMessageHandler? handler = null)
    {
        handler ??= new NotFoundHandler();

        var signalrProxy  = new Mock<IClientProxy>();
        var clientsMock   = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(signalrProxy.Object);

        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var statusService = new Mock<IFileAnalysisStatusService>();

        var consumer = new DfmAnalysisReadyConsumer(
            hubMock.Object,
            CreateHttpClientFactory(handler).Object,
            statusService.Object,
            NullLogger<DfmAnalysisReadyConsumer>.Instance);

        return (consumer, signalrProxy, statusService);
    }

    // ── Tests: caching ────────────────────────────────────────────────────────

    [Fact]
    public async Task Consume_ValidEvent_CachesDfmReports()
    {
        var (consumer, _, statusService) = CreateConsumer();
        var ctx = MakeCtx(BuildEvent());

        await consumer.Consume(ctx.Object);

        statusService.Verify(s => s.SetDfmReportsAsync(
            StoragePath,
            It.IsAny<object>(),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Consume_NullPayload_DoesNotCacheOrBroadcast()
    {
        var (consumer, signalrProxy, statusService) = CreateConsumer();
        var ctx = MakeCtx(BuildEvent(nullPayload: true));

        await consumer.Consume(ctx.Object);

        statusService.Verify(s => s.SetDfmReportsAsync(
            It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        signalrProxy.Verify(p => p.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_EmptyStoragePath_DoesNotCacheOrBroadcast()
    {
        var (consumer, signalrProxy, statusService) = CreateConsumer();
        var ctx = MakeCtx(BuildEvent(storagePath: ""));

        await consumer.Consume(ctx.Object);

        statusService.Verify(s => s.SetDfmReportsAsync(
            It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        signalrProxy.Verify(p => p.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Tests: SignalR broadcast ───────────────────────────────────────────────

    [Fact]
    public async Task Consume_ValidEvent_BroadcastsDfmAnalysisReadyToCorrectGroup()
    {
        var capturedMethod = string.Empty;
        var capturedArgs   = Array.Empty<object?>();

        var signalrProxy = new Mock<IClientProxy>();
        signalrProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((method, args, _) =>
            {
                capturedMethod = method;
                capturedArgs   = args;
            })
            .Returns(Task.CompletedTask);

        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Group($"file:{StoragePath}")).Returns(signalrProxy.Object);

        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var consumer = new DfmAnalysisReadyConsumer(
            hubMock.Object,
            CreateHttpClientFactory(new NotFoundHandler()).Object,
            new Mock<IFileAnalysisStatusService>().Object,
            NullLogger<DfmAnalysisReadyConsumer>.Instance);

        await consumer.Consume(MakeCtx(BuildEvent()).Object);

        Assert.Equal("DfmAnalysisReady", capturedMethod);
        Assert.Single(capturedArgs);
        var broadcastPayload = capturedArgs[0] as DfmAnalysisReadyPayload;
        Assert.NotNull(broadcastPayload);
        Assert.Equal(StoragePath, broadcastPayload.StoragePath);
    }

    [Fact]
    public async Task Consume_ValidEvent_BroadcastFdmReportIsDeserialised()
    {
        DfmAnalysisReadyPayload? broadcastPayload = null;

        var signalrProxy = new Mock<IClientProxy>();
        signalrProxy
            .Setup(p => p.SendCoreAsync("DfmAnalysisReady", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
                broadcastPayload = args[0] as DfmAnalysisReadyPayload)
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(signalrProxy.Object);
        var hub = new Mock<IHubContext<NotificationHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        var consumer = new DfmAnalysisReadyConsumer(
            hub.Object,
            CreateHttpClientFactory(new NotFoundHandler()).Object,
            new Mock<IFileAnalysisStatusService>().Object,
            NullLogger<DfmAnalysisReadyConsumer>.Instance);

        await consumer.Consume(MakeCtx(BuildEvent()).Object);

        Assert.NotNull(broadcastPayload?.FdmReport);
        Assert.Equal(2, broadcastPayload.FdmReport!.ThinWallCount);
        Assert.Equal(5, broadcastPayload.FdmReport.OverhangFaceCount);
        Assert.True(broadcastPayload.FdmReport.SupportRequired);
    }

    // ── Tests: overlay URL signing ────────────────────────────────────────────

    [Fact]
    public async Task Consume_NoOverlayPaths_BroadcastsNullOverlayUrls()
    {
        DfmAnalysisReadyPayload? broadcastPayload = null;

        var signalrProxy = new Mock<IClientProxy>();
        signalrProxy
            .Setup(p => p.SendCoreAsync("DfmAnalysisReady", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
                broadcastPayload = args[0] as DfmAnalysisReadyPayload)
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(signalrProxy.Object);
        var hub = new Mock<IHubContext<NotificationHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        var consumer = new DfmAnalysisReadyConsumer(
            hub.Object,
            CreateHttpClientFactory(new NotFoundHandler()).Object,
            new Mock<IFileAnalysisStatusService>().Object,
            NullLogger<DfmAnalysisReadyConsumer>.Instance);

        await consumer.Consume(MakeCtx(BuildEvent(overlayPaths: null)).Object);

        Assert.Null(broadcastPayload?.OverlayUrls);
    }

    [Fact]
    public async Task Consume_WithOverlayPaths_SignsAndBroadcastsUrls()
    {
        DfmAnalysisReadyPayload? broadcastPayload = null;

        var signalrProxy = new Mock<IClientProxy>();
        signalrProxy
            .Setup(p => p.SendCoreAsync("DfmAnalysisReady", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
                broadcastPayload = args[0] as DfmAnalysisReadyPayload)
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(signalrProxy.Object);
        var hub = new Mock<IHubContext<NotificationHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        var consumer = new DfmAnalysisReadyConsumer(
            hub.Object,
            CreateHttpClientFactory(new PassthroughSignedUrlHandler()).Object,
            new Mock<IFileAnalysisStatusService>().Object,
            NullLogger<DfmAnalysisReadyConsumer>.Instance);

        var overlayPaths = new Dictionary<string, string>
        {
            ["FDM__thin_wall"] = "projects/abc/model.stl_dfm_fdm__thin_wall.glb",
            ["FDM__overhang"]  = "projects/abc/model.stl_dfm_fdm__overhang.glb",
        };

        await consumer.Consume(MakeCtx(BuildEvent(overlayPaths: overlayPaths)).Object);

        Assert.NotNull(broadcastPayload?.OverlayUrls);
        Assert.Equal(2, broadcastPayload.OverlayUrls!.Count);
        Assert.True(broadcastPayload.OverlayUrls.ContainsKey("FDM__thin_wall"));
        Assert.True(broadcastPayload.OverlayUrls.ContainsKey("FDM__overhang"));
        // PassthroughSignedUrlHandler echoes the GCS path as the signed URL
        Assert.Equal("projects/abc/model.stl_dfm_fdm__thin_wall.glb",
            broadcastPayload.OverlayUrls["FDM__thin_wall"]);

        // Raw overlay paths should also be forwarded for draft persistence
        Assert.NotNull(broadcastPayload.OverlayPaths);
        Assert.Equal(2, broadcastPayload.OverlayPaths!.Count);
        Assert.Equal("projects/abc/model.stl_dfm_fdm__thin_wall.glb",
            broadcastPayload.OverlayPaths["FDM__thin_wall"]);
    }

    [Fact]
    public async Task Consume_OverlaySigningFails_StillBroadcastsWithoutOverlays()
    {
        DfmAnalysisReadyPayload? broadcastPayload = null;

        var signalrProxy = new Mock<IClientProxy>();
        signalrProxy
            .Setup(p => p.SendCoreAsync("DfmAnalysisReady", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
                broadcastPayload = args[0] as DfmAnalysisReadyPayload)
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(signalrProxy.Object);
        var hub = new Mock<IHubContext<NotificationHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        // 404 → signing yields empty URL → filtered out → overlayUrls is empty dict (not null)
        var consumer = new DfmAnalysisReadyConsumer(
            hub.Object,
            CreateHttpClientFactory(new NotFoundHandler()).Object,
            new Mock<IFileAnalysisStatusService>().Object,
            NullLogger<DfmAnalysisReadyConsumer>.Instance);

        var overlayPaths = new Dictionary<string, string>
        {
            ["FDM__thin_wall"] = "projects/abc/model.stl_dfm_fdm__thin_wall.glb",
        };

        await consumer.Consume(MakeCtx(BuildEvent(overlayPaths: overlayPaths)).Object);

        // SignalR broadcast must still happen even if overlay signing returns empty
        Assert.NotNull(broadcastPayload);
        Assert.Equal(StoragePath, broadcastPayload.StoragePath);
    }
}
