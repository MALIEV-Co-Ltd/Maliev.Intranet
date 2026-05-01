using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// MassTransit consumer that handles <see cref="FileMetricsReadyEvent"/> messages published by GeometryService.
/// Stores dimensions immediately after mesh metrics are computed — before preview images are generated.
/// </summary>
public class FileMetricsReadyConsumer : IConsumer<FileMetricsReadyEvent>
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IFileAnalysisStatusService _analysisStatusService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FileMetricsReadyConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FileMetricsReadyConsumer"/>.
    /// </summary>
    public FileMetricsReadyConsumer(
        IHubContext<NotificationHub> hub,
        IFileAnalysisStatusService analysisStatusService,
        IHttpClientFactory httpClientFactory,
        ILogger<FileMetricsReadyConsumer> logger)
    {
        _hub = hub;
        _analysisStatusService = analysisStatusService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Processes an incoming <see cref="FileMetricsReadyEvent"/> and immediately surfaces dimensions via SignalR.
    /// </summary>
    public async Task Consume(ConsumeContext<FileMetricsReadyEvent> context)
    {
        if (context.Message?.Payload == null)
        {
            _logger.LogWarning("FileMetricsReadyConsumer: received null message or payload");
            return;
        }

        var payload = context.Message.Payload;
        _logger.LogInformation(
            "FileMetricsReadyConsumer: received event for file {FileId}, storagePath={StoragePath}",
            payload.FileId, payload.StoragePath);

        var uploadClient = new UploadServiceClient("UploadServiceClient.Consumer", _httpClientFactory);
        var storagePath = await ResolveCurrentStoragePathAsync(
            uploadClient,
            payload.FileId,
            payload.StoragePath,
            context.CancellationToken);

        var dimensions = payload.Metrics?.BoundingBox is { } bb
            ? new FileAnalysisDimensionsDto
            {
                X = bb.X,
                Y = bb.Y,
                Z = bb.Z,
                VolumeMm3 = payload.Metrics.VolumeCm3 * 1000,
            }
            : null;

        var isManifold = payload.Metrics?.IsManifold ?? true;
        var nonManifoldReason = payload.Metrics?.NonManifoldReason;
        var nonManifoldFaceCount = payload.Metrics?.NonManifoldFaceCount;

        await _analysisStatusService.SetProcessingAsync(storagePath, context.CancellationToken);

        if (dimensions != null)
        {
            await _analysisStatusService.SetDimensionsAsync(
                storagePath, dimensions, isManifold, nonManifoldReason, nonManifoldFaceCount, context.CancellationToken);

            _logger.LogInformation(
                "FileMetricsReadyConsumer: stored dimensions for key={StoragePath}, manifold={IsManifold}",
                storagePath, isManifold);
        }

        // Convert body metadata to SignalR format
        var bodies = payload.Bodies?.Select(b => new SignalRBodyInfo(
            b.Index,
            b.Name,
            b.VolumeCm3,
            new SignalRBBox(
                b.BboxMin?.X ?? 0,
                b.BboxMin?.Y ?? 0,
                b.BboxMin?.Z ?? 0),
            new SignalRBBox(
                b.BboxMax?.X ?? 0,
                b.BboxMax?.Y ?? 0,
                b.BboxMax?.Z ?? 0)
        )).ToList();

        var signalRPayload = new FileAnalysisCompletedPayload(
            StoragePath: storagePath,
            UploadId: payload.FileId,
            ThumbnailUrl: null,
            HiResThumbnailUrl: null,
            Dimensions: dimensions != null
                ? new FileAnalysisDimensions(dimensions.X, dimensions.Y, dimensions.Z, dimensions.VolumeMm3)
                : null,
            PreviewUrls: null,
            Failed: false,
            ErrorCode: null,
            BodyCount: payload.BodyCount,
            Bodies: bodies,
            NonManifoldReason: nonManifoldReason,
            NonManifoldFaceCount: nonManifoldFaceCount);

        await _hub.Clients.Group($"file:{storagePath}").SendAsync(
            "FileAnalysisCompleted",
            signalRPayload,
            context.CancellationToken);

        _logger.LogInformation(
            "FileMetricsReadyConsumer: pushed dimensions via SignalR for {StoragePath}",
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
            "FileMetricsReadyConsumer: file {FileId} moved while analysis was in flight; using current storage path {CurrentPath} instead of event path {EventPath}.",
            fileId,
            currentPath,
            eventStoragePath);
        return currentPath;
    }
}
