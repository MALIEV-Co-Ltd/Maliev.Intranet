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
            "PreviewImagesGeneratedConsumer: received event for fileId={FileId}, storagePath={StoragePath}, failed={Failed}",
            payload.FileId, payload.StoragePath, payload.Failed);

        var storagePath = payload.StoragePath;
        try
        {
            var uploadClient = CreateUploadClient();
            storagePath = await ResolveCurrentStoragePathAsync(
                uploadClient,
                payload.FileId,
                payload.StoragePath,
                context.CancellationToken);

            var previews = payload.PreviewImages;

            _logger.LogInformation(
                "RAW preview paths from GeometryService - FrontSmall: {Front}, ThumbnailSmall: {Iso}, TopSmall: {Top}, BottomSmall: {Bottom}, LeftSmall: {Left}, RightSmall: {Right}, BackSmall: {Back}",
                previews.FrontSmall, previews.ThumbnailSmall, previews.TopSmall, previews.BottomSmall, previews.LeftSmall, previews.RightSmall, previews.BackSmall);

            async Task<ResolvedPreviewUrl> ResolveUrlAsync(string assetName, string? path)
            {
                if (string.IsNullOrEmpty(path))
                    return new ResolvedPreviewUrl(assetName, null, null, false);

                var url = await uploadClient.GetDownloadUrlByPathAsync(path, context.CancellationToken, expirationMinutes: 10080);
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogWarning(
                        "PreviewImagesGeneratedConsumer: signed URL resolution failed for {AssetName} path={Path}",
                        assetName, path);
                    return new ResolvedPreviewUrl(assetName, path, null, true);
                }

                _logger.LogDebug(
                    "PreviewImagesGeneratedConsumer: signed URL resolved for {AssetName} path={Path}",
                    assetName, path);
                return new ResolvedPreviewUrl(assetName, path, url, false);
            }

            var resolvedUrls = await Task.WhenAll(
                ResolveUrlAsync(nameof(previews.FrontSmall), previews.FrontSmall),
                ResolveUrlAsync(nameof(previews.ThumbnailSmall), previews.ThumbnailSmall),
                ResolveUrlAsync(nameof(previews.ThumbnailLarge), previews.ThumbnailLarge),
                ResolveUrlAsync(nameof(previews.BackSmall), previews.BackSmall),
                ResolveUrlAsync(nameof(previews.LeftSmall), previews.LeftSmall),
                ResolveUrlAsync(nameof(previews.RightSmall), previews.RightSmall),
                ResolveUrlAsync(nameof(previews.TopSmall), previews.TopSmall),
                ResolveUrlAsync(nameof(previews.BottomSmall), previews.BottomSmall));

            var resultsByAsset = resolvedUrls.ToDictionary(result => result.AssetName, StringComparer.Ordinal);
            var frontResult = resultsByAsset[nameof(previews.FrontSmall)];
            var thumbnailSmallResult = resultsByAsset[nameof(previews.ThumbnailSmall)];
            var thumbnailLargeResult = resultsByAsset[nameof(previews.ThumbnailLarge)];
            var backResult = resultsByAsset[nameof(previews.BackSmall)];
            var leftResult = resultsByAsset[nameof(previews.LeftSmall)];
            var rightResult = resultsByAsset[nameof(previews.RightSmall)];
            var topResult = resultsByAsset[nameof(previews.TopSmall)];
            var bottomResult = resultsByAsset[nameof(previews.BottomSmall)];

            bool anyUrlFailed = frontResult.Failed || thumbnailSmallResult.Failed || thumbnailLargeResult.Failed
                || backResult.Failed || leftResult.Failed || rightResult.Failed || topResult.Failed || bottomResult.Failed;

            _logger.LogInformation(
                "Resolved preview URLs for storagePath={StoragePath}: resolvedCount={ResolvedCount}, anyFailed={AnyFailed}",
                storagePath, resolvedUrls.Count(result => !string.IsNullOrEmpty(result.Url)), anyUrlFailed);

            var previewUrlsDto = new FileAnalysisPreviewUrlsDto
            {
                FrontSmall = frontResult.Url,
                BackSmall = backResult.Url,
                LeftSmall = leftResult.Url,
                RightSmall = rightResult.Url,
                TopSmall = topResult.Url,
                BottomSmall = bottomResult.Url,
                ThumbnailSmall = thumbnailSmallResult.Url,
                ThumbnailLargeUrl = thumbnailLargeResult.Url,
                ThumbnailSmallGcsPath = previews.ThumbnailSmall,
                ThumbnailLargeGcsPath = previews.ThumbnailLarge,
            };

            bool overallFailed = payload.Failed || anyUrlFailed;

            // Pass thumbnailUrl: null so SetPreviewUrlsAsync preserves the isometric URL
            // already stored by SmallThumbnailReadyConsumer (uses existing ?? fallback internally).
            await _analysisStatusService.SetPreviewUrlsAsync(
                storagePath, previewUrlsDto, thumbnailUrl: null, thumbnailLargeResult.Url, context.CancellationToken);

            if (overallFailed)
            {
                _logger.LogWarning(
                    "Preview image generation failed for {StoragePath} - transitioning to preview-failed state (payloadFailed={PayloadFailed}, urlFailed={UrlFailed})",
                    storagePath, payload.Failed, anyUrlFailed);
                await _analysisStatusService.SetPreviewUrlsFailedAsync(storagePath, context.CancellationToken);
            }
            else
            {
                await _analysisStatusService.SetPreviewUrlsCompletedAsync(storagePath, context.CancellationToken);
            }

            var signalRPayload = new FileAnalysisCompletedPayload(
                StoragePath: storagePath,
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

            await SendToFileGroupsAsync(payload.StoragePath, storagePath, signalRPayload, context.CancellationToken);

            _logger.LogInformation(
                "Pushed preview URLs for {StoragePath} via SignalR (failed={Failed})",
                storagePath, payload.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PreviewImagesGeneratedConsumer failed for {StoragePath}",
                storagePath);
            await _analysisStatusService.SetAnalysisFailedAsync(
                storagePath, "preview-consumer-error", context.CancellationToken);
            throw;
        }
    }

    private async Task<string> ResolveCurrentStoragePathAsync(
        UploadServiceClient uploadClient,
        string? fileId,
        string eventStoragePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return eventStoragePath;
        }

        var currentPath = await uploadClient.GetStoragePathAsync(fileId, cancellationToken);
        if (string.IsNullOrWhiteSpace(currentPath) ||
            string.Equals(currentPath, eventStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            return eventStoragePath;
        }

        _logger.LogInformation(
            "PreviewImagesGeneratedConsumer: file {FileId} moved while previews were in flight; using current storage path {CurrentPath} instead of event path {EventPath}.",
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

        foreach (var path in groupPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            await _hub.Clients.Group($"file:{path}").SendAsync(
                "FileAnalysisCompleted",
                payload,
                cancellationToken);
        }
    }

    private sealed record ResolvedPreviewUrl(string AssetName, string? StoragePath, string? Url, bool Failed);
}
