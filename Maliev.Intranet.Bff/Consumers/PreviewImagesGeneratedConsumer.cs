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

            async Task<(string? Url, bool Failed)> ResolveUrlAsync(string? path)
            {
                if (string.IsNullOrEmpty(path))
                    return (null, false);
                var url = await CreateUploadClient().GetDownloadUrlByPathAsync(path, context.CancellationToken);
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogWarning(
                        "PreviewImagesGeneratedConsumer: signed URL resolution failed for path={Path}",
                        path);
                    return (null, true);
                }
                return (url, false);
            }

            var frontResult          = await ResolveUrlAsync(previews.FrontSmall);
            var thumbnailSmallResult = await ResolveUrlAsync(previews.ThumbnailSmall);
            var thumbnailLargeResult = await ResolveUrlAsync(previews.ThumbnailLarge);
            var backResult           = await ResolveUrlAsync(previews.BackSmall);
            var leftResult           = await ResolveUrlAsync(previews.LeftSmall);
            var rightResult          = await ResolveUrlAsync(previews.RightSmall);
            var topResult            = await ResolveUrlAsync(previews.TopSmall);
            var bottomResult         = await ResolveUrlAsync(previews.BottomSmall);

            bool anyUrlFailed = frontResult.Failed || thumbnailSmallResult.Failed || thumbnailLargeResult.Failed
                || backResult.Failed || leftResult.Failed || rightResult.Failed || topResult.Failed || bottomResult.Failed;

            _logger.LogInformation(
                "ResolveUrl results for storagePath={StoragePath} - FrontSmall: {FrontUrl}, ThumbnailSmall: {IsoUrl}, ThumbnailLarge: {Iso1000Url}, anyFailed={AnyFailed}",
                payload.StoragePath, frontResult.Url, thumbnailSmallResult.Url, thumbnailLargeResult.Url, anyUrlFailed);

            var previewUrlsDto = new FileAnalysisPreviewUrlsDto
            {
                FrontSmall            = frontResult.Url,
                BackSmall             = backResult.Url,
                LeftSmall             = leftResult.Url,
                RightSmall            = rightResult.Url,
                TopSmall              = topResult.Url,
                BottomSmall           = bottomResult.Url,
                ThumbnailSmall        = thumbnailSmallResult.Url,
                ThumbnailLargeUrl      = thumbnailLargeResult.Url,
                ThumbnailSmallGcsPath  = previews.ThumbnailSmall,
                ThumbnailLargeGcsPath  = previews.ThumbnailLarge,
            };

            bool overallFailed = payload.Failed || anyUrlFailed;

            // Pass thumbnailUrl: null so SetPreviewUrlsAsync preserves the isometric URL
            // already stored by SmallThumbnailReadyConsumer (uses existing ?? fallback internally).
            await _analysisStatusService.SetPreviewUrlsAsync(
                payload.StoragePath, previewUrlsDto, thumbnailUrl: null, thumbnailLargeResult.Url, context.CancellationToken);

            if (overallFailed)
            {
                _logger.LogWarning(
                    "Preview image generation failed for {StoragePath} - transitioning to preview-failed state (payloadFailed={PayloadFailed}, urlFailed={UrlFailed})",
                    payload.StoragePath, payload.Failed, anyUrlFailed);
                await _analysisStatusService.SetPreviewUrlsFailedAsync(payload.StoragePath, context.CancellationToken);
            }
            else
            {
                await _analysisStatusService.SetPreviewUrlsCompletedAsync(payload.StoragePath, context.CancellationToken);
            }

            var signalRPayload = new FileAnalysisCompletedPayload(
                StoragePath: payload.StoragePath,
                UploadId: null,
                ThumbnailUrl: null,
                HiResThumbnailUrl: thumbnailLargeResult.Url,
                Dimensions: null,
                PreviewUrls: new FileAnalysisPreviewUrls(
                    FrontSmall: frontResult.Url,
                    BackSmall: previewUrlsDto.BackSmall,
                    LeftSmall: previewUrlsDto.LeftSmall,
                    RightSmall: previewUrlsDto.RightSmall,
                    TopSmall: previewUrlsDto.TopSmall,
                    BottomSmall: previewUrlsDto.BottomSmall,
                    ThumbnailSmall: thumbnailSmallResult.Url,
                    ThumbnailLarge: thumbnailLargeResult.Url,
                    ThumbnailSmallGcsPath: previews.ThumbnailSmall,
                    ThumbnailLargeGcsPath: previews.ThumbnailLarge),
                Failed: overallFailed,
                ErrorCode: overallFailed ? (payload.Failed ? "preview-generation-failed" : "preview-url-resolution-failed") : null);

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
