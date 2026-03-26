using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// MassTransit consumer that handles <see cref="SmallThumbnailReadyEvent"/> messages published by GeometryService.
/// Resolves the signed thumbnail URL and delivers it to the relevant client via SignalR.
/// </summary>
public class SmallThumbnailReadyConsumer : IConsumer<SmallThumbnailReadyEvent>
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmallThumbnailReadyConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SmallThumbnailReadyConsumer"/>.
    /// </summary>
    public SmallThumbnailReadyConsumer(
        IHubContext<NotificationHub> hub,
        IHttpClientFactory httpClientFactory,
        ILogger<SmallThumbnailReadyConsumer> logger)
    {
        _hub = hub;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private UploadServiceClient CreateUploadClient() =>
        new("UploadServiceClient.Consumer", _httpClientFactory);

    /// <summary>
    /// Processes an incoming <see cref="SmallThumbnailReadyEvent"/> and delivers the thumbnail URL to the client via SignalR.
    /// </summary>
    public async Task Consume(ConsumeContext<SmallThumbnailReadyEvent> context)
    {
        if (context.Message?.Payload == null)
        {
            _logger.LogWarning("SmallThumbnailReadyConsumer: received null message or payload");
            return;
        }

        var payload = context.Message.Payload;
        _logger.LogInformation(
            "SmallThumbnailReadyConsumer: received event for file {FileId}, storagePath={StoragePath}",
            payload.FileId, payload.StoragePath);

        var thumbnailUrl = await CreateUploadClient()
            .GetDownloadUrlByPathAsync(payload.ThumbnailStoragePath, context.CancellationToken)
            ?? payload.ThumbnailStoragePath;

        var signalRPayload = new FileAnalysisCompletedPayload(
            StoragePath: payload.StoragePath,
            UploadId: payload.FileId,
            ThumbnailUrl: thumbnailUrl,
            HiResThumbnailUrl: null,
            Dimensions: null,
            PreviewUrls: null,
            Failed: false,
            ErrorCode: null);

        await _hub.Clients.Group($"file:{payload.StoragePath}").SendAsync(
            "FileAnalysisCompleted",
            signalRPayload,
            context.CancellationToken);

        _logger.LogInformation(
            "SmallThumbnailReadyConsumer: pushed thumbnail URL via SignalR for {StoragePath}",
            payload.StoragePath);
    }
}
