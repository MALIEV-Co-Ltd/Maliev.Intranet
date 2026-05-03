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

        var uploadClient = CreateUploadClient();
        var storagePath = await ResolveCurrentStoragePathAsync(
            uploadClient,
            payload.FileId,
            payload.StoragePath,
            context.CancellationToken);

        var thumbnailUrl = await uploadClient
            .GetDownloadUrlByPathAsync(payload.ThumbnailStoragePath, context.CancellationToken);

        bool failed = string.IsNullOrEmpty(thumbnailUrl);

        var signalRPayload = new FileAnalysisCompletedPayload(
            StoragePath: storagePath,
            UploadId: payload.FileId,
            ThumbnailUrl: failed ? null : thumbnailUrl,
            HiResThumbnailUrl: null,
            Dimensions: null,
            PreviewUrls: null,
            Failed: failed,
            ErrorCode: failed ? "thumbnail-url-resolution-failed" : null);

        await SendToFileGroupsAsync(payload.StoragePath, storagePath, signalRPayload, context.CancellationToken);

        _logger.LogInformation(
            "SmallThumbnailReadyConsumer: pushed thumbnail URL via SignalR for {StoragePath}",
            storagePath);
    }

    private async Task<string> ResolveCurrentStoragePathAsync(
        UploadServiceClient uploadClient,
        string fileId,
        string eventStoragePath,
        CancellationToken cancellationToken)
    {
        var currentPath = await uploadClient.GetStoragePathAsync(fileId, cancellationToken);
        if (string.IsNullOrWhiteSpace(currentPath) ||
            string.Equals(currentPath, eventStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            return eventStoragePath;
        }

        _logger.LogInformation(
            "SmallThumbnailReadyConsumer: file {FileId} moved while analysis was in flight; using current storage path {CurrentPath} instead of event path {EventPath}.",
            fileId,
            currentPath,
            eventStoragePath);
        return currentPath;
    }

    private async Task SendToFileGroupsAsync(
        string eventStoragePath,
        string currentStoragePath,
        FileAnalysisCompletedPayload payload,
        CancellationToken cancellationToken)
    {
        var groupPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            eventStoragePath,
            currentStoragePath
        };

        foreach (var storagePath in groupPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            await _hub.Clients.Group($"file:{storagePath}").SendAsync(
                "FileAnalysisCompleted",
                payload,
                cancellationToken);
        }
    }
}
