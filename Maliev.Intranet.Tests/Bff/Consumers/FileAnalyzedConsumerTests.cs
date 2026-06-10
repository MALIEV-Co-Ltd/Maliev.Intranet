using System.Net;
using System.Text;

using Maliev.Intranet.Bff.Consumers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Consumers;

/// <summary>
/// Preview images are generated locally in the browser from the viewer source.
/// When a <see cref="FileAnalyzedEvent"/> delivers a resolvable viewer artifact,
/// the consumer must mark preview processing terminal so polling clients reach
/// the Ready state without waiting for server preview events that no longer come.
/// </summary>
public sealed class FileAnalyzedConsumerTests
{
    private const string StoragePath = "projects/project-1/part.step";
    private const string GlbStoragePath = "projects/project-1/part.step_viewer.glb";

    [Fact]
    public async Task Consume_WhenViewerUrlResolves_MarksPreviewProcessingCompleted()
    {
        var statusService = CreateAnalysisStatusService(existingStatus: null);
        var consumer = CreateConsumer(statusService, signedUrlAvailable: true);

        await consumer.Consume(CreateConsumeContext().Object);

        statusService.Verify(
            service => service.SetAnalysisCompletedAsync(
                StoragePath,
                GlbStoragePath,
                It.Is<string?>(url => !string.IsNullOrEmpty(url)),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
        statusService.Verify(
            service => service.SetPreviewUrlsCompletedAsync(StoragePath, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_WhenViewerUrlCannotBeResolved_DoesNotMarkPreviewsCompleted()
    {
        var statusService = CreateAnalysisStatusService(existingStatus: null);
        var consumer = CreateConsumer(statusService, signedUrlAvailable: false);

        await consumer.Consume(CreateConsumeContext().Object);

        statusService.Verify(
            service => service.SetPreviewUrlsCompletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_WhenPreviewsAlreadyCompleted_DoesNotMarkAgain()
    {
        var statusService = CreateAnalysisStatusService(new FileAnalysisStatusDto
        {
            UploadId = StoragePath,
            Status = FileAnalysisStatus.Processing,
            PreviewProcessingStatus = PreviewProcessingStatus.Completed,
        });
        var consumer = CreateConsumer(statusService, signedUrlAvailable: true);

        await consumer.Consume(CreateConsumeContext().Object);

        statusService.Verify(
            service => service.SetPreviewUrlsCompletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static FileAnalyzedConsumer CreateConsumer(
        Mock<IFileAnalysisStatusService> statusService,
        bool signedUrlAvailable)
    {
        return new FileAnalyzedConsumer(
            CreateHub().Object,
            CreateUploadHttpClientFactory(signedUrlAvailable).Object,
            statusService.Object,
            NullLogger<FileAnalyzedConsumer>.Instance);
    }

    private static Mock<IHttpClientFactory> CreateUploadHttpClientFactory(bool signedUrlAvailable)
    {
        var factory = new Mock<IHttpClientFactory>();
        var client = new HttpClient(new MockHttpMessageHandler((request, _) =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/by-path/signed-url", StringComparison.Ordinal))
            {
                return Task.FromResult(signedUrlAvailable
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            """{"signedUrl":"https://signed.test/viewer.glb"}""",
                            Encoding.UTF8,
                            "application/json"),
                    }
                    : new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"storagePath":"{{StoragePath}}"}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        }))
        {
            BaseAddress = new Uri("https://upload-service.test"),
        };

        factory
            .Setup(item => item.CreateClient("UploadServiceClient.Consumer"))
            .Returns(client);
        return factory;
    }

    private static Mock<IFileAnalysisStatusService> CreateAnalysisStatusService(
        FileAnalysisStatusDto? existingStatus)
    {
        var service = new Mock<IFileAnalysisStatusService>();
        service
            .Setup(item => item.GetStatusAsync(StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStatus);
        return service;
    }

    private static Mock<IHubContext<NotificationHub>> CreateHub()
    {
        var proxy = new Mock<IClientProxy>();
        proxy
            .Setup(item => item.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(item => item.Group(It.IsAny<string>())).Returns(proxy.Object);

        var hub = new Mock<IHubContext<NotificationHub>>();
        hub.SetupGet(item => item.Clients).Returns(clients.Object);
        return hub;
    }

    private static Mock<ConsumeContext<FileAnalyzedEvent>> CreateConsumeContext()
    {
        var payload = new FileAnalyzedEventPayload() with
        {
            FileId = "file-1",
            StoragePath = StoragePath,
            GlbStoragePath = GlbStoragePath,
            ViewerStoragePath = GlbStoragePath,
            ViewerFileExtension = ".glb",
        };

        var context = new Mock<ConsumeContext<FileAnalyzedEvent>>();
        context.SetupGet(item => item.CancellationToken).Returns(CancellationToken.None);
        context.SetupGet(item => item.Message).Returns(new FileAnalyzedEvent() with { Payload = payload });
        return context;
    }
}
