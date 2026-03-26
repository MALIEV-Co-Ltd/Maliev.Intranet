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
                await _analysisStatusService.SetAnalysisCompletedAsync(gcsStoragePath, payload.GlbStoragePath, context.CancellationToken);

                _logger.LogInformation(
                    "FileAnalyzedConsumer: marked analysis completed for key={CacheKey}, GlbStoragePath={GlbStoragePath}",
                    gcsStoragePath, payload.GlbStoragePath);

                string? glbUrl = null;
                if (!string.IsNullOrEmpty(payload.GlbStoragePath))
                {
                    glbUrl = await CreateUploadClient()
                        .GetDownloadUrlByPathAsync(payload.GlbStoragePath, context.CancellationToken);
                    // No fallback — if signed URL resolution fails, Failed = true on the client
                }

                await _hub.Clients.Group($"file:{gcsStoragePath}").SendAsync("GlbReady", new GlbReadyPayload(
                    StoragePath: gcsStoragePath,
                    GlbUrl: glbUrl,
                    Failed: string.IsNullOrEmpty(glbUrl)
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
