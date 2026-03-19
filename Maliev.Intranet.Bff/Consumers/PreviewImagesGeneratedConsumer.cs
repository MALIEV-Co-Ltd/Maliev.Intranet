using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// MassTransit consumer that handles <see cref="PreviewImagesGeneratedEvent"/> messages published by GeometryService.
/// Stores analysis status for polling and broadcasts preview URLs to SignalR clients.
/// </summary>
public class PreviewImagesGeneratedConsumer : IConsumer<PreviewImagesGeneratedEvent>
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileAnalysisStatusService _analysisStatusService;
    private readonly ILogger<PreviewImagesGeneratedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewImagesGeneratedConsumer"/>.
    /// </summary>
    public PreviewImagesGeneratedConsumer(
        IHubContext<NotificationHub> hub,
        IHttpClientFactory httpClientFactory,
        IFileAnalysisStatusService analysisStatusService,
        ILogger<PreviewImagesGeneratedConsumer> logger)
    {
        _hub = hub;
        _httpClientFactory = httpClientFactory;
        _analysisStatusService = analysisStatusService;
        _logger = logger;
    }

    private UploadServiceClient CreateUploadClient() =>
        new("UploadServiceClient.Consumer", _httpClientFactory);

    /// <summary>
    /// Processes an incoming <see cref="PreviewImagesGeneratedEvent"/> and stores preview URLs for polling.
    /// </summary>
    /// <param name="context">The MassTransit consume context.</param>
    public async Task Consume(ConsumeContext<PreviewImagesGeneratedEvent> context)
    {
        if (context.Message?.Payload == null)
        {
            _logger.LogWarning("Received null message or payload - possible deserialization issue");
            return;
        }

        var payload = context.Message.Payload;
        _logger.LogInformation(
            "Received PreviewImagesGeneratedEvent for path {StoragePath}, failed={Failed}",
            payload.StoragePath, payload.Failed);

        try
        {
            var previews = payload.PreviewImages;
            var uploadClient = CreateUploadClient();

            async Task<string?> ResolveUrlAsync(string? path) =>
                string.IsNullOrEmpty(path)
                    ? null
                    : await uploadClient.GetDownloadUrlByPathAsync(path, context.CancellationToken) ?? path;

            var frontUrl = await ResolveUrlAsync(previews.Front);
            var isoUrl   = await ResolveUrlAsync(previews.Iso);
            var thumbnailUrl = isoUrl ?? frontUrl;

            var previewUrlsDto = new FileAnalysisPreviewUrlsDto
            {
                Front  = frontUrl,
                Back   = await ResolveUrlAsync(previews.Back),
                Left   = await ResolveUrlAsync(previews.Left),
                Right  = await ResolveUrlAsync(previews.Right),
                Top    = await ResolveUrlAsync(previews.Top),
                Bottom = await ResolveUrlAsync(previews.Bottom),
                Iso    = isoUrl,
            };

            await _analysisStatusService.SetPreviewUrlsAsync(
                payload.StoragePath, previewUrlsDto, thumbnailUrl, context.CancellationToken);

            if (payload.Failed)
            {
                _logger.LogWarning(
                    "Preview image generation failed for {StoragePath} - transitioning to preview-failed state",
                    payload.StoragePath);
                await _analysisStatusService.SetPreviewUrlsFailedAsync(payload.StoragePath, context.CancellationToken);
            }
            else
            {
                await _analysisStatusService.SetPreviewUrlsCompletedAsync(payload.StoragePath, context.CancellationToken);
            }

            var signalRPayload = new FileAnalysisCompletedPayload(
                StoragePath: payload.StoragePath,
                UploadId: null,
                ThumbnailUrl: thumbnailUrl,
                Dimensions: null,
                PreviewUrls: new FileAnalysisPreviewUrls(
                    Front: frontUrl,
                    Back: previewUrlsDto.Back,
                    Left: previewUrlsDto.Left,
                    Right: previewUrlsDto.Right,
                    Top: previewUrlsDto.Top,
                    Bottom: previewUrlsDto.Bottom,
                    Iso: isoUrl),
                Failed: payload.Failed,
                ErrorCode: payload.Failed ? "preview-generation-failed" : null);

            await _hub.Clients.All.SendAsync(
                "FileAnalysisCompleted",
                signalRPayload,
                context.CancellationToken);

            _logger.LogInformation(
                "Pushed preview URLs for {StoragePath} via SignalR (failed={Failed})",
                payload.StoragePath, payload.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PreviewImagesGeneratedConsumer failed for {StoragePath}",
                payload.StoragePath);
            await _analysisStatusService.SetAnalysisFailedAsync(
                payload.StoragePath, "preview-consumer-error", context.CancellationToken);
            throw;
        }
    }
}
