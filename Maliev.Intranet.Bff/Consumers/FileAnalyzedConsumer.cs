using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
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

        var gcsStoragePath = payload.StoragePath;

        _logger.LogInformation(
            "FileAnalyzedConsumer: storagePath={StoragePath}, GlbPath={GlbPath}",
            gcsStoragePath, payload.GlbStoragePath);

        if (!string.IsNullOrEmpty(gcsStoragePath))
        {
            try
            {
                // Generate signed URL first, then store it in cache
                string? glbUrl = null;
                if (!string.IsNullOrEmpty(payload.GlbStoragePath))
                {
                    glbUrl = await CreateUploadClient()
                        .GetDownloadUrlByPathAsync(payload.GlbStoragePath, context.CancellationToken);
                }

                var existing = await _analysisStatusService.GetStatusAsync(gcsStoragePath, context.CancellationToken);
                await _analysisStatusService.SetAnalysisCompletedAsync(
                    gcsStoragePath,
                    payload.GlbStoragePath ?? existing?.GlbStoragePath,
                    glbUrl,
                    payload.DfmReport ?? existing?.DfmReport,
                    context.CancellationToken);

                _logger.LogInformation(
                    "FileAnalyzedConsumer: marked analysis completed for key={CacheKey}, GlbStoragePath={GlbStoragePath}, GlbSignedUrl={HasGlbSignedUrl}, hasDfmReport={HasDfmReport}",
                    gcsStoragePath, payload.GlbStoragePath, !string.IsNullOrEmpty(glbUrl), payload.DfmReport != null);

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

                await _hub.Clients.Group($"file:{gcsStoragePath}").SendAsync("GlbReady", new GlbReadyPayload(
                    StoragePath: gcsStoragePath,
                    GlbUrl: glbUrl,
                    Failed: string.IsNullOrEmpty(glbUrl),
                    BodyCount: payload.BodyCount,
                    Bodies: bodies
                ), context.CancellationToken);
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
}
