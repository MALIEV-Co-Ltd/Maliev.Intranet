using Maliev.Intranet.Bff.Clients;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileAnalysisStatusService _analysisStatusService;
    private readonly ILogger<FileAnalyzedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FileAnalyzedConsumer"/>.
    /// </summary>
    public FileAnalyzedConsumer(
        IHubContext<NotificationHub> hub,
        IHttpClientFactory httpClientFactory,
        IFileAnalysisStatusService analysisStatusService,
        ILogger<FileAnalyzedConsumer> logger)
    {
        _hub = hub;
        _httpClientFactory = httpClientFactory;
        _analysisStatusService = analysisStatusService;
        _logger = logger;
    }

    private UploadServiceClient CreateUploadClient() =>
        new("UploadServiceClient.Consumer", _httpClientFactory);

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
            "Received FileAnalyzedEvent for file {FileId}, manifold={IsManifold}, GlbPath={GlbPath}, ThumbPath={ThumbPath}",
            payload.FileId, payload.Metrics?.IsManifold, payload.GlbStoragePath, payload.ThumbnailStoragePath);

        var uploadClient = CreateUploadClient();
        var gcsStoragePath = await ResolveCurrentStoragePathAsync(
            uploadClient,
            payload.FileId,
            payload.StoragePath,
            context.CancellationToken);
        var glbStoragePath = await ResolveCurrentGlbPathAsync(
            uploadClient,
            payload.StoragePath,
            gcsStoragePath,
            payload.GlbStoragePath,
            context.CancellationToken);

        _logger.LogInformation(
            "FileAnalyzedConsumer: storagePath={StoragePath}, GlbPath={GlbPath}",
            gcsStoragePath, glbStoragePath);

        if (!string.IsNullOrEmpty(gcsStoragePath))
        {
            try
            {
                // Generate signed URL first, then store it in cache
                string? glbUrl = null;
                if (!string.IsNullOrEmpty(glbStoragePath))
                {
                    glbUrl = await uploadClient.GetDownloadUrlByPathAsync(
                        glbStoragePath,
                        context.CancellationToken);
                }

                var existing = await _analysisStatusService.GetStatusAsync(gcsStoragePath, context.CancellationToken);
                if (payload.Metrics?.BoundingBox is { } bb)
                {
                    await _analysisStatusService.SetDimensionsAsync(
                        gcsStoragePath,
                        new FileAnalysisDimensionsDto
                        {
                            X = bb.X,
                            Y = bb.Y,
                            Z = bb.Z,
                            VolumeMm3 = payload.Metrics.VolumeCm3 * 1000
                        },
                        payload.Metrics.IsManifold,
                        cancellationToken: context.CancellationToken);
                }

                await _analysisStatusService.SetAnalysisCompletedAsync(
                    gcsStoragePath,
                    glbStoragePath ?? existing?.GlbStoragePath,
                    glbUrl,
                    payload.DfmReport ?? existing?.DfmReport,
                    context.CancellationToken);

                _logger.LogInformation(
                    "FileAnalyzedConsumer: marked analysis completed for key={CacheKey}, GlbStoragePath={GlbStoragePath}, GlbSignedUrl={HasGlbSignedUrl}, hasDfmReport={HasDfmReport}",
                    gcsStoragePath, glbStoragePath, !string.IsNullOrEmpty(glbUrl), payload.DfmReport != null);

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

                var signalRPayload = new GlbReadyPayload(
                    StoragePath: gcsStoragePath,
                    GlbUrl: glbUrl,
                    Failed: string.IsNullOrEmpty(glbUrl),
                    BodyCount: payload.BodyCount,
                    Bodies: bodies
                );

                await SendToFileGroupsAsync(
                    payload.StoragePath,
                    gcsStoragePath,
                    signalRPayload,
                    context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "FileAnalyzedConsumer failed for {StoragePath}", payload.StoragePath);
                if (!string.IsNullOrEmpty(gcsStoragePath))
                {
                    await _analysisStatusService.SetAnalysisFailedAsync(
                        gcsStoragePath, "glb-consumer-error", context.CancellationToken);
                }
                throw;
            }
        }
        else
        {
            _logger.LogWarning(
                "FileAnalyzedConsumer: missing storagePath — skipping status update for FileId={FileId}",
                payload.FileId);
        }
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
            "FileAnalyzedConsumer: file {FileId} moved while analysis was in flight; using current storage path {CurrentPath} instead of event path {EventPath}.",
            fileId,
            currentPath,
            eventStoragePath);
        return currentPath;
    }

    private async Task<string?> ResolveCurrentGlbPathAsync(
        UploadServiceClient uploadClient,
        string eventStoragePath,
        string currentStoragePath,
        string? eventGlbPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(eventGlbPath) ||
            string.Equals(eventStoragePath, currentStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            return eventGlbPath;
        }

        var expectedEventGlbPath = eventStoragePath + "_viewer.glb";
        if (!string.Equals(eventGlbPath, expectedEventGlbPath, StringComparison.OrdinalIgnoreCase))
        {
            return eventGlbPath;
        }

        var currentGlbPath = currentStoragePath + "_viewer.glb";
        if (await uploadClient.CopyFileAsync(eventGlbPath, currentGlbPath, cancellationToken))
        {
            _logger.LogInformation(
                "FileAnalyzedConsumer: migrated late GLB artifact {OldGlbPath} to {NewGlbPath}.",
                eventGlbPath,
                currentGlbPath);
            return currentGlbPath;
        }

        _logger.LogWarning(
            "FileAnalyzedConsumer: late GLB artifact {OldGlbPath} could not be copied to {NewGlbPath}; using original artifact path.",
            eventGlbPath,
            currentGlbPath);
        return eventGlbPath;
    }

    private async Task SendToFileGroupsAsync(
        string eventStoragePath,
        string currentStoragePath,
        GlbReadyPayload payload,
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
                "GlbReady",
                payload,
                cancellationToken);
        }
    }
}
