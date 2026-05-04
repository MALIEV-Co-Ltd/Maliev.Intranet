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
    private const string AliasKeyPrefix = "file-analysis-alias:";
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
            NonManifoldReason = existing?.NonManifoldReason,
            NonManifoldFaceCount = existing?.NonManifoldFaceCount,
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
    public Task SetDimensionsAsync(string uploadId, FileAnalysisDimensionsDto dimensions, bool isManifold, string? nonManifoldReason = null, int? nonManifoldFaceCount = null, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = existing?.Status ?? FileAnalysisStatus.Processing,
            Dimensions = dimensions,
            IsManifold = isManifold,
            NonManifoldReason = nonManifoldReason,
            NonManifoldFaceCount = nonManifoldFaceCount,
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
        var mergedPreviewUrls = MergePreviewUrls(existing?.PreviewUrls, previewUrls);
        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = existing?.Status ?? FileAnalysisStatus.Processing,
            Dimensions = existing?.Dimensions,
            IsManifold = existing?.IsManifold,
            NonManifoldReason = existing?.NonManifoldReason,
            NonManifoldFaceCount = existing?.NonManifoldFaceCount,
            ThumbnailUrl = thumbnailUrl ?? existing?.ThumbnailUrl,
            HiResThumbnailUrl = hiResThumbnailUrl ?? existing?.HiResThumbnailUrl,
            PreviewUrls = mergedPreviewUrls,
            PreviewProcessingStatus = PreviewProcessingStatus.Processing,
            ErrorCode = existing?.ErrorCode,
            ProcessedAt = existing?.ProcessedAt ?? DateTimeOffset.UtcNow,
            DfmReport = existing?.DfmReport,
        };
        Set(uploadId, status);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetThumbnailAsync(string uploadId, string? thumbnailUrl, string? thumbnailStoragePath = null, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        var previewUrls = existing?.PreviewUrls is null
            ? new FileAnalysisPreviewUrlsDto()
            : existing.PreviewUrls;

        previewUrls = previewUrls with
        {
            ThumbnailSmall = thumbnailUrl ?? previewUrls.ThumbnailSmall,
            ThumbnailSmallGcsPath = thumbnailStoragePath ?? previewUrls.ThumbnailSmallGcsPath
        };

        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = existing?.Status ?? FileAnalysisStatus.Processing,
            Dimensions = existing?.Dimensions,
            IsManifold = existing?.IsManifold,
            NonManifoldReason = existing?.NonManifoldReason,
            NonManifoldFaceCount = existing?.NonManifoldFaceCount,
            ThumbnailUrl = thumbnailUrl ?? existing?.ThumbnailUrl,
            HiResThumbnailUrl = existing?.HiResThumbnailUrl,
            PreviewUrls = previewUrls,
            GlbStoragePath = existing?.GlbStoragePath,
            GlbSignedUrl = existing?.GlbSignedUrl,
            PreviewProcessingStatus = existing?.PreviewProcessingStatus ?? PreviewProcessingStatus.Pending,
            ErrorCode = existing?.ErrorCode,
            ProcessedAt = existing?.ProcessedAt ?? DateTimeOffset.UtcNow,
            DfmReport = existing?.DfmReport,
        };

        Set(uploadId, status);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetAnalysisCompletedAsync(string uploadId, string? glbStoragePath = null, string? glbSignedUrl = null, object? dfmReport = null, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        var status = new FileAnalysisStatusDto
        {
            UploadId = uploadId,
            Status = FileAnalysisStatus.Completed,
            Dimensions = existing?.Dimensions,
            IsManifold = existing?.IsManifold,
            NonManifoldReason = existing?.NonManifoldReason,
            NonManifoldFaceCount = existing?.NonManifoldFaceCount,
            ThumbnailUrl = existing?.ThumbnailUrl,
            HiResThumbnailUrl = existing?.HiResThumbnailUrl,
            PreviewUrls = existing?.PreviewUrls,
            GlbStoragePath = glbStoragePath ?? existing?.GlbStoragePath,
            GlbSignedUrl = glbSignedUrl ?? existing?.GlbSignedUrl,
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
            NonManifoldReason = existing?.NonManifoldReason,
            NonManifoldFaceCount = existing?.NonManifoldFaceCount,
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
            NonManifoldReason = existing?.NonManifoldReason,
            NonManifoldFaceCount = existing?.NonManifoldFaceCount,
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
            NonManifoldReason = existing?.NonManifoldReason,
            NonManifoldFaceCount = existing?.NonManifoldFaceCount,
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
    public Task SetDfmReportsAsync(string uploadId, object dfmReports, CancellationToken cancellationToken = default)
    {
        var existing = Get(uploadId);
        if (existing == null)
        {
            _logger.LogDebug("SetDfmReportsAsync: no entry found for key={Key}, skipping", uploadId);
            return Task.CompletedTask;
        }

        var status = new FileAnalysisStatusDto
        {
            UploadId = existing.UploadId,
            Status = existing.Status,
            Dimensions = existing.Dimensions,
            IsManifold = existing.IsManifold,
            NonManifoldReason = existing.NonManifoldReason,
            NonManifoldFaceCount = existing.NonManifoldFaceCount,
            ThumbnailUrl = existing.ThumbnailUrl,
            HiResThumbnailUrl = existing.HiResThumbnailUrl,
            PreviewUrls = existing.PreviewUrls,
            GlbStoragePath = existing.GlbStoragePath,
            PreviewProcessingStatus = existing.PreviewProcessingStatus,
            ErrorCode = existing.ErrorCode,
            ProcessedAt = existing.ProcessedAt,
            DfmReport = dfmReports,
        };
        Set(uploadId, status);
        _logger.LogInformation("SetDfmReportsAsync: updated DFM reports for key={Key}", uploadId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<FileAnalysisStatusDto?> GetStatusAsync(string uploadId, CancellationToken cancellationToken = default)
    {
        var status = Get(uploadId);
        if (status == null && TryGetAlias(uploadId, out var destinationStoragePath))
        {
            status = Get(destinationStoragePath);
        }

        if (status == null)
        {
            _logger.LogDebug("GetStatusAsync: no entry found for key={Key}", uploadId);
        }
        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public Task RegisterStoragePathAliasAsync(string sourceStoragePath, string destinationStoragePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceStoragePath) || string.IsNullOrWhiteSpace(destinationStoragePath) ||
            string.Equals(sourceStoragePath, destinationStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        _cache.Set(GetAliasKey(sourceStoragePath), destinationStoragePath, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = 1
        });

        var existing = Get(sourceStoragePath);
        if (existing != null)
        {
            SetDirect(destinationStoragePath, RewriteStatusForAlias(existing, sourceStoragePath, destinationStoragePath));
        }

        _logger.LogInformation(
            "Registered file analysis storage-path alias {SourceStoragePath} -> {DestinationStoragePath}",
            sourceStoragePath,
            destinationStoragePath);

        return Task.CompletedTask;
    }

    private void Set(string uploadId, FileAnalysisStatusDto status)
    {
        SetDirect(uploadId, status);
        if (TryGetAlias(uploadId, out var destinationStoragePath))
        {
            SetDirect(destinationStoragePath, RewriteStatusForAlias(status, uploadId, destinationStoragePath));
        }
    }

    private void SetDirect(string uploadId, FileAnalysisStatusDto status)
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

    private bool TryGetAlias(string sourceStoragePath, out string destinationStoragePath)
    {
        return _cache.TryGetValue(GetAliasKey(sourceStoragePath), out destinationStoragePath!);
    }

    private static string GetAliasKey(string sourceStoragePath) => $"{AliasKeyPrefix}{sourceStoragePath}";

    private static FileAnalysisStatusDto RewriteStatusForAlias(
        FileAnalysisStatusDto status,
        string sourceStoragePath,
        string destinationStoragePath)
    {
        var previewUrls = status.PreviewUrls is null
            ? null
            : status.PreviewUrls with
            {
                ThumbnailSmallGcsPath = RewriteStoragePath(status.PreviewUrls.ThumbnailSmallGcsPath, sourceStoragePath, destinationStoragePath),
                ThumbnailLargeGcsPath = RewriteStoragePath(status.PreviewUrls.ThumbnailLargeGcsPath, sourceStoragePath, destinationStoragePath)
            };

        return status with
        {
            UploadId = destinationStoragePath,
            PreviewUrls = previewUrls,
            GlbStoragePath = RewriteStoragePath(status.GlbStoragePath, sourceStoragePath, destinationStoragePath)
        };
    }

    private static FileAnalysisPreviewUrlsDto MergePreviewUrls(FileAnalysisPreviewUrlsDto? existing, FileAnalysisPreviewUrlsDto incoming)
    {
        if (existing is null)
            return incoming;

        return incoming with
        {
            FrontSmall = incoming.FrontSmall ?? existing.FrontSmall,
            BackSmall = incoming.BackSmall ?? existing.BackSmall,
            LeftSmall = incoming.LeftSmall ?? existing.LeftSmall,
            RightSmall = incoming.RightSmall ?? existing.RightSmall,
            TopSmall = incoming.TopSmall ?? existing.TopSmall,
            BottomSmall = incoming.BottomSmall ?? existing.BottomSmall,
            ThumbnailSmall = incoming.ThumbnailSmall ?? existing.ThumbnailSmall,
            ThumbnailLargeUrl = incoming.ThumbnailLargeUrl ?? existing.ThumbnailLargeUrl,
            ThumbnailSmallGcsPath = incoming.ThumbnailSmallGcsPath ?? existing.ThumbnailSmallGcsPath,
            ThumbnailLargeGcsPath = incoming.ThumbnailLargeGcsPath ?? existing.ThumbnailLargeGcsPath
        };
    }

    private static string? RewriteStoragePath(string? storagePath, string sourceStoragePath, string destinationStoragePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return storagePath;

        if (string.Equals(storagePath, sourceStoragePath, StringComparison.OrdinalIgnoreCase))
            return destinationStoragePath;

        if (!storagePath.StartsWith(sourceStoragePath, StringComparison.OrdinalIgnoreCase))
            return storagePath;

        return destinationStoragePath + storagePath[sourceStoragePath.Length..];
    }

    /// <inheritdoc />
    public async Task MigrateGlbStoragePathAsync(string oldStoragePath, string newStoragePath, CancellationToken cancellationToken = default)
    {
        var oldKey = $"{CacheKeyPrefix}{oldStoragePath}";
        if (!_cache.TryGetValue(oldKey, out FileAnalysisStatusDto? existing))
        {
            _logger.LogDebug("MigrateGlbStoragePathAsync: no cache entry found for old key={OldKey}", oldKey);
            return;
        }

        if (string.IsNullOrEmpty(existing?.GlbStoragePath))
        {
            _logger.LogDebug("MigrateGlbStoragePathAsync: no GlbStoragePath to migrate for entry {OldKey}", oldKey);
            return;
        }

        var oldGlbViewerPath = oldStoragePath + "_viewer.glb";
        if (!existing.GlbStoragePath.EndsWith(oldGlbViewerPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "MigrateGlbStoragePathAsync: GlbStoragePath={GlbPath} does not match expected viewer convention for old path {OldPath}. Manual migration may be required.",
                existing.GlbStoragePath, oldStoragePath);
            return;
        }

        var newGlbViewerPath = newStoragePath + "_viewer.glb";
        var updated = new FileAnalysisStatusDto
        {
            UploadId = existing.UploadId,
            Status = existing.Status,
            Dimensions = existing.Dimensions,
            IsManifold = existing.IsManifold,
            NonManifoldReason = existing.NonManifoldReason,
            NonManifoldFaceCount = existing.NonManifoldFaceCount,
            ThumbnailUrl = existing.ThumbnailUrl,
            HiResThumbnailUrl = existing.HiResThumbnailUrl,
            PreviewUrls = existing.PreviewUrls,
            GlbStoragePath = newGlbViewerPath,
            GlbSignedUrl = existing.GlbSignedUrl,
            PreviewProcessingStatus = existing.PreviewProcessingStatus,
            ErrorCode = existing.ErrorCode,
            ProcessedAt = existing.ProcessedAt,
            DfmReport = existing.DfmReport,
        };

        Set(newStoragePath, updated);
        _logger.LogInformation(
            "MigrateGlbStoragePathAsync: migrated cache entry {OldKey} → {NewKey}, GlbStoragePath {OldGlbPath} → {NewGlbPath}",
            oldKey, $"{CacheKeyPrefix}{newStoragePath}", existing.GlbStoragePath, newGlbViewerPath);
    }

    /// <inheritdoc />
    public Task CloneStatusAsync(
        string sourceStoragePath,
        string destinationStoragePath,
        string? destinationThumbnailSmallUrl = null,
        string? destinationThumbnailLargeUrl = null,
        string? destinationThumbnailSmallGcsPath = null,
        string? destinationThumbnailLargeGcsPath = null,
        string? destinationGlbStoragePath = null,
        string? destinationGlbSignedUrl = null,
        CancellationToken cancellationToken = default)
    {
        var oldKey = $"{CacheKeyPrefix}{sourceStoragePath}";
        if (!_cache.TryGetValue(oldKey, out FileAnalysisStatusDto? existing) || existing is null)
        {
            _logger.LogDebug("CloneStatusAsync: no cache entry found for source key={OldKey}", oldKey);
            return Task.CompletedTask;
        }

        var previewUrls = existing.PreviewUrls is null
            ? null
            : existing.PreviewUrls with
            {
                ThumbnailSmall = destinationThumbnailSmallUrl ?? existing.PreviewUrls.ThumbnailSmall,
                ThumbnailLargeUrl = destinationThumbnailLargeUrl ?? existing.PreviewUrls.ThumbnailLargeUrl,
                ThumbnailSmallGcsPath = destinationThumbnailSmallGcsPath ?? existing.PreviewUrls.ThumbnailSmallGcsPath,
                ThumbnailLargeGcsPath = destinationThumbnailLargeGcsPath ?? existing.PreviewUrls.ThumbnailLargeGcsPath
            };

        var cloned = new FileAnalysisStatusDto
        {
            UploadId = destinationStoragePath,
            Status = existing.Status,
            Dimensions = existing.Dimensions,
            IsManifold = existing.IsManifold,
            NonManifoldReason = existing.NonManifoldReason,
            NonManifoldFaceCount = existing.NonManifoldFaceCount,
            ThumbnailUrl = destinationThumbnailSmallUrl ?? existing.ThumbnailUrl,
            HiResThumbnailUrl = destinationThumbnailLargeUrl ?? existing.HiResThumbnailUrl,
            PreviewUrls = previewUrls,
            GlbStoragePath = destinationGlbStoragePath ?? existing.GlbStoragePath,
            GlbSignedUrl = destinationGlbSignedUrl ?? existing.GlbSignedUrl,
            PreviewProcessingStatus = existing.PreviewProcessingStatus,
            ErrorCode = existing.ErrorCode,
            ProcessedAt = existing.ProcessedAt,
            DfmReport = existing.DfmReport,
        };

        Set(destinationStoragePath, cloned);
        _logger.LogInformation(
            "CloneStatusAsync: cloned cache entry {OldKey} to {NewKey}",
            oldKey,
            $"{CacheKeyPrefix}{destinationStoragePath}");

        return Task.CompletedTask;
    }
}
