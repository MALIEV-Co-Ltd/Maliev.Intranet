using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// MassTransit consumer that handles <see cref="FileAnalyzedEvent"/> messages published by GeometryService.
/// Stores analysis status for polling and broadcasts to connected Blazor clients via SignalR.
/// </summary>
public class FileAnalyzedConsumer : IConsumer<FileAnalyzedEvent>
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IFileAnalysisStatusService _analysisStatusService;
    private readonly ILogger<FileAnalyzedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FileAnalyzedConsumer"/>.
    /// </summary>
    public FileAnalyzedConsumer(
        IHubContext<NotificationHub> hub,
        IFileAnalysisStatusService analysisStatusService,
        ILogger<FileAnalyzedConsumer> logger)
    {
        _hub = hub;
        _analysisStatusService = analysisStatusService;
        _logger = logger;
    }

    /// <summary>
    /// Processes an incoming <see cref="FileAnalyzedEvent"/> and stores status for polling.
    /// </summary>
    /// <param name="context">The MassTransit consume context.</param>
    public async Task Consume(ConsumeContext<FileAnalyzedEvent> context)
    {
        if (context.Message?.Payload == null)
        {
            _logger.LogWarning("Received null message or payload - possible deserialization issue");
            return;
        }

        var payload = context.Message.Payload;
        _logger.LogInformation(
            "Received FileAnalyzedEvent for file {FileId}, manifold={IsManifold}",
            payload.FileId, payload.Metrics?.IsManifold);

        var dimensions = payload.Metrics?.BoundingBox is { } bb
            ? new FileAnalysisDimensionsDto
              {
                  X         = bb.X,
                  Y         = bb.Y,
                  Z         = bb.Z,
                  VolumeMm3 = payload.Metrics.VolumeCm3 * 1000,
              }
            : null;

        string? gcsStoragePath = null;
        if (!string.IsNullOrEmpty(payload.GlbStoragePath) &&
            payload.GlbStoragePath.EndsWith("_viewer.glb", StringComparison.OrdinalIgnoreCase))
        {
            gcsStoragePath = payload.GlbStoragePath[..^"_viewer.glb".Length];
        }
        else if (!string.IsNullOrEmpty(payload.ThumbnailStoragePath) &&
                 payload.ThumbnailStoragePath.EndsWith("_thumb.png", StringComparison.OrdinalIgnoreCase))
        {
            gcsStoragePath = payload.ThumbnailStoragePath[..^"_thumb.png".Length];
        }

        _logger.LogInformation(
            "Derived GCS storage path: {GcsStoragePath} from GlbPath={GlbPath}, ThumbPath={ThumbPath}",
            gcsStoragePath, payload.GlbStoragePath, payload.ThumbnailStoragePath);

        var isManifold = payload.Metrics?.IsManifold ?? true;

        if (gcsStoragePath != null)
        {
            await _analysisStatusService.SetProcessingAsync(gcsStoragePath, context.CancellationToken);

            if (dimensions != null)
            {
                await _analysisStatusService.SetDimensionsAsync(gcsStoragePath, dimensions, isManifold, context.CancellationToken);
            }

            var signalRPayload = new FileAnalysisCompletedPayload(
                StoragePath: gcsStoragePath,
                UploadId: payload.FileId,
                ThumbnailUrl: null,
                Dimensions: dimensions != null ? new FileAnalysisDimensions(dimensions.X, dimensions.Y, dimensions.Z, dimensions.VolumeMm3) : null,
                PreviewUrls: null,
                Failed: false,
                ErrorCode: null);

            await _hub.Clients.All.SendAsync(
                "FileAnalysisCompleted",
                signalRPayload,
                context.CancellationToken);
        }
    }
}
