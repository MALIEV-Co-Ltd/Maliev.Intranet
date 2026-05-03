using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// MassTransit consumer that handles <see cref="FileAnalysisFailedEvent"/> messages published by GeometryService.
/// Updates analysis status cache and notifies connected Blazor clients via SignalR so the UI unblocks.
/// </summary>
public class FileAnalysisFailedConsumer : IConsumer<FileAnalysisFailedEvent>
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileAnalysisStatusService _analysisStatusService;
    private readonly ILogger<FileAnalysisFailedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FileAnalysisFailedConsumer"/>.
    /// </summary>
    public FileAnalysisFailedConsumer(
        IHubContext<NotificationHub> hub,
        IHttpClientFactory httpClientFactory,
        IFileAnalysisStatusService analysisStatusService,
        ILogger<FileAnalysisFailedConsumer> logger)
    {
        _hub = hub;
        _httpClientFactory = httpClientFactory;
        _analysisStatusService = analysisStatusService;
        _logger = logger;
    }

    private UploadServiceClient CreateUploadClient() =>
        new("UploadServiceClient.Consumer", _httpClientFactory);

    /// <summary>
    /// Processes an incoming <see cref="FileAnalysisFailedEvent"/>, marks the file as failed in the
    /// status cache, and pushes a failure notification to the client via SignalR.
    /// </summary>
    public async Task Consume(ConsumeContext<FileAnalysisFailedEvent> context)
    {
        if (context.Message?.Payload == null)
        {
            _logger.LogWarning("FileAnalysisFailedConsumer: received null message or payload");
            return;
        }

        var payload = context.Message.Payload;
        _logger.LogWarning(
            "FileAnalysisFailedConsumer: geometry analysis failed for file {FileId}, storagePath={StoragePath}, errorCode={ErrorCode}",
            payload.FileId, payload.StoragePath, payload.ErrorCode);

        var storagePath = await ResolveCurrentStoragePathAsync(
            CreateUploadClient(),
            payload.FileId,
            payload.StoragePath,
            context.CancellationToken);

        await _analysisStatusService.SetAnalysisFailedAsync(
            payload.StoragePath, payload.ErrorCode, context.CancellationToken);

        if (!string.Equals(storagePath, payload.StoragePath, StringComparison.OrdinalIgnoreCase))
        {
            await _analysisStatusService.SetAnalysisFailedAsync(
                storagePath, payload.ErrorCode, context.CancellationToken);
        }

        var signalRPayload = new FileAnalysisCompletedPayload(
            StoragePath: storagePath,
            UploadId: payload.FileId,
            ThumbnailUrl: null,
            HiResThumbnailUrl: null,
            Dimensions: null,
            PreviewUrls: null,
            Failed: true,
            ErrorCode: payload.ErrorCode);

        await SendToFileGroupsAsync(payload.StoragePath, storagePath, signalRPayload, context.CancellationToken);

        _logger.LogInformation(
            "FileAnalysisFailedConsumer: pushed failure notification via SignalR for {StoragePath}",
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
            "FileAnalysisFailedConsumer: file {FileId} moved while analysis was in flight; using current storage path {CurrentPath} instead of event path {EventPath}.",
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
