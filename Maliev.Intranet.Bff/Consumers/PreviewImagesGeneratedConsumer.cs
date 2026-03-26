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
            "PreviewImagesGeneratedConsumer: received event for storagePath={StoragePath}, failed={Failed}",
            payload.StoragePath, payload.Failed);

        try
        {
            var previews = payload.PreviewImages;

            _logger.LogInformation(
                "RAW preview paths from GeometryService - FrontSmall: {Front}, ThumbnailSmall: {Iso}, TopSmall: {Top}, BottomSmall: {Bottom}, LeftSmall: {Left}, RightSmall: {Right}, BackSmall: {Back}",
                previews.FrontSmall, previews.ThumbnailSmall, previews.TopSmall, previews.BottomSmall, previews.LeftSmall, previews.RightSmall, previews.BackSmall);

            async Task<string?> ResolveUrlAsync(string? path)
            {
                if (string.IsNullOrEmpty(path))
                    return null;
                return await CreateUploadClient().GetDownloadUrlByPathAsync(path, context.CancellationToken)
                       ?? path;  // fallback: store raw storage path when signed URL fails
            }

            var frontUrl           = await ResolveUrlAsync(previews.FrontSmall);
            var thumbnailSmallUrl  = await ResolveUrlAsync(previews.ThumbnailSmall);
            var thumbnailLargeUrl  = await ResolveUrlAsync(previews.ThumbnailLarge);
            var thumbnailUrl       = thumbnailSmallUrl ?? frontUrl;

            _logger.LogInformation(
                "ResolveUrl results for storagePath={StoragePath} - FrontSmall: {FrontUrl}, ThumbnailSmall: {IsoUrl}, ThumbnailLarge: {Iso1000Url}, Thumbnail: {ThumbUrl}, RawFront: {RawFront}, RawThumbnailSmall: {RawIso}",
                payload.StoragePath, frontUrl, thumbnailSmallUrl, thumbnailLargeUrl, thumbnailUrl, previews.FrontSmall, previews.ThumbnailSmall);

            var previewUrlsDto = new FileAnalysisPreviewUrlsDto
            {
                FrontSmall       = frontUrl,
                BackSmall        = await ResolveUrlAsync(previews.BackSmall),
                LeftSmall        = await ResolveUrlAsync(previews.LeftSmall),
                RightSmall       = await ResolveUrlAsync(previews.RightSmall),
                TopSmall         = await ResolveUrlAsync(previews.TopSmall),
                BottomSmall      = await ResolveUrlAsync(previews.BottomSmall),
                ThumbnailSmall   = thumbnailSmallUrl,
                ThumbnailLargeUrl = thumbnailLargeUrl,
            };

            await _analysisStatusService.SetPreviewUrlsAsync(
                payload.StoragePath, previewUrlsDto, thumbnailUrl, thumbnailLargeUrl, context.CancellationToken);

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
                HiResThumbnailUrl: thumbnailLargeUrl,
                Dimensions: null,
                PreviewUrls: new FileAnalysisPreviewUrls(
                    FrontSmall: frontUrl,
                    BackSmall: previewUrlsDto.BackSmall,
                    LeftSmall: previewUrlsDto.LeftSmall,
                    RightSmall: previewUrlsDto.RightSmall,
                    TopSmall: previewUrlsDto.TopSmall,
                    BottomSmall: previewUrlsDto.BottomSmall,
                    ThumbnailSmall: thumbnailSmallUrl,
                    ThumbnailLarge: thumbnailLargeUrl),
                Failed: payload.Failed,
                ErrorCode: payload.Failed ? "preview-generation-failed" : null);

            await _hub.Clients.Group($"file:{payload.StoragePath}").SendAsync(
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
