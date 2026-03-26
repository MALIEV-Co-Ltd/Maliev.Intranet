using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Singleton implementation of <see cref="IFileAnalysisStatusService"/> that stores
/// file analysis status in <see cref="IMemoryCache"/> (process-local, singleton-backed).
/// </summary>
public sealed class FileAnalysisStatusService : IFileAnalysisStatusService
{
    private const string CacheKeyPrefix = "file-analysis:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;
    private readonly ILogger<FileAnalysisStatusService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FileAnalysisStatusService"/>.
    /// </summary>
    /// <param name="cache">The singleton memory cache.</param>
    /// <param name="logger">The logger instance.</param>
    public FileAnalysisStatusService(IMemoryCache cache, ILogger<FileAnalysisStatusService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SetProcessingAsync(string uploadId, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = FileAnalysisStatus.Processing,
            Dimensions = existing?.Dimensions,
            IsManifold = existing?.IsManifold,
            ThumbnailUrl = existing?.ThumbnailUrl,
            HiResThumbnailUrl = existing?.HiResThumbnailUrl,
            PreviewUrls = existing?.PreviewUrls,
            PreviewProcessingStatus = existing?.PreviewProcessingStatus ?? PreviewProcessingStatus.Pending,
            ErrorCode = existing?.ErrorCode,
            ProcessedAt = existing?.ProcessedAt ?? DateTimeOffset.UtcNow,
            DfmReport = existing?.DfmReport,
        };
        Set(uploadId, status);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetDimensionsAsync(string uploadId, FileAnalysisDimensionsDto dimensions, bool isManifold, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = existing?.Status ?? FileAnalysisStatus.Processing,
            Dimensions = dimensions,
            IsManifold = isManifold,
            ThumbnailUrl = existing?.ThumbnailUrl,
            HiResThumbnailUrl = existing?.HiResThumbnailUrl,
            PreviewUrls = existing?.PreviewUrls,
            PreviewProcessingStatus = existing?.PreviewProcessingStatus ?? PreviewProcessingStatus.Pending,
            ErrorCode = existing?.ErrorCode,
            ProcessedAt = existing?.ProcessedAt ?? DateTimeOffset.UtcNow,
            DfmReport = existing?.DfmReport,
        };
        Set(uploadId, status);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetPreviewUrlsAsync(string uploadId, FileAnalysisPreviewUrlsDto previewUrls, string? thumbnailUrl, string? hiResThumbnailUrl = null, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = existing?.Status ?? FileAnalysisStatus.Processing,
            Dimensions = existing?.Dimensions,
            IsManifold = existing?.IsManifold,
            ThumbnailUrl = thumbnailUrl ?? existing?.ThumbnailUrl,
            HiResThumbnailUrl = hiResThumbnailUrl ?? existing?.HiResThumbnailUrl,
            PreviewUrls = previewUrls,
            PreviewProcessingStatus = PreviewProcessingStatus.Processing,
            ErrorCode = existing?.ErrorCode,
            ProcessedAt = existing?.ProcessedAt ?? DateTimeOffset.UtcNow,
            DfmReport = existing?.DfmReport,
        };
        Set(uploadId, status);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetAnalysisCompletedAsync(string uploadId, string? glbStoragePath = null, object? dfmReport = null, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = FileAnalysisStatus.Completed,
            Dimensions = existing?.Dimensions,
            IsManifold = existing?.IsManifold,
            ThumbnailUrl = existing?.ThumbnailUrl,
            HiResThumbnailUrl = existing?.HiResThumbnailUrl,
            PreviewUrls = existing?.PreviewUrls,
            GlbStoragePath = glbStoragePath ?? existing?.GlbStoragePath,
            PreviewProcessingStatus = existing?.PreviewProcessingStatus ?? PreviewProcessingStatus.Pending,
            ErrorCode = null,
            ProcessedAt = DateTimeOffset.UtcNow,
            DfmReport = dfmReport ?? existing?.DfmReport,
        };
        Set(uploadId, status);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetPreviewUrlsCompletedAsync(string uploadId, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = FileAnalysisStatus.Completed,
            Dimensions = existing?.Dimensions,
            IsManifold = existing?.IsManifold,
            ThumbnailUrl = existing?.ThumbnailUrl,
            HiResThumbnailUrl = existing?.HiResThumbnailUrl,
            PreviewUrls = existing?.PreviewUrls,
            PreviewProcessingStatus = PreviewProcessingStatus.Completed,
            ErrorCode = existing?.ErrorCode,
            ProcessedAt = DateTimeOffset.UtcNow,
            DfmReport = existing?.DfmReport,
        };
        Set(uploadId, status);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetPreviewUrlsFailedAsync(string uploadId, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = existing?.Status ?? FileAnalysisStatus.Completed,
            Dimensions = existing?.Dimensions,
            IsManifold = existing?.IsManifold,
            ThumbnailUrl = existing?.ThumbnailUrl,
            HiResThumbnailUrl = existing?.HiResThumbnailUrl,
            PreviewUrls = existing?.PreviewUrls,
            PreviewProcessingStatus = PreviewProcessingStatus.Failed,
            ErrorCode = "preview-generation-failed",
            ProcessedAt = DateTimeOffset.UtcNow,
            DfmReport = existing?.DfmReport,
        };
        Set(uploadId, status);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetAnalysisFailedAsync(string uploadId, string errorCode, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = FileAnalysisStatus.Failed,
            Dimensions = existing?.Dimensions,
            IsManifold = existing?.IsManifold,
            ThumbnailUrl = existing?.ThumbnailUrl,
            HiResThumbnailUrl = existing?.HiResThumbnailUrl,
            PreviewUrls = existing?.PreviewUrls,
            PreviewProcessingStatus = existing?.PreviewProcessingStatus ?? PreviewProcessingStatus.Pending,
            ErrorCode = errorCode,
            ProcessedAt = DateTimeOffset.UtcNow,
            DfmReport = existing?.DfmReport,
        };
        Set(uploadId, status);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<FileAnalysisStatusDto?> GetStatusAsync(string uploadId, CancellationToken cancellationToken = default)
    {
        var status = Get(uploadId);
        if (status == null)
        {
            _logger.LogDebug("GetStatusAsync: no entry found for key={Key}", uploadId);
        }
        return Task.FromResult(status);
    }

    private void Set(string uploadId, FileAnalysisStatusDto status)
    {
        var key = $"{CacheKeyPrefix}{uploadId}";
        _cache.Set(key, status, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = 1
        });
    }

    private FileAnalysisStatusDto? Get(string uploadId)
    {
        _cache.TryGetValue($"{CacheKeyPrefix}{uploadId}", out FileAnalysisStatusDto? status);
        return status;
    }
}
