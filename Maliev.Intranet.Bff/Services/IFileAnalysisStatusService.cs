using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Service for storing and retrieving file analysis status.
/// Used by polling-based clients to check geometry analysis completion.
/// </summary>
public interface IFileAnalysisStatusService
{
    /// <summary>
    /// Sets the initial processing status for a file.
    /// </summary>
    Task SetProcessingAsync(string uploadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the dimensions and manifold state after geometry analysis completes.
    /// </summary>
    Task SetDimensionsAsync(string uploadId, FileAnalysisDimensionsDto dimensions, bool isManifold, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the preview URLs after preview images are generated.
    /// </summary>
    Task SetPreviewUrlsAsync(string uploadId, FileAnalysisPreviewUrlsDto previewUrls, string? thumbnailUrl, string? hiResThumbnailUrl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the geometry analysis as completed and stores the optional GLB storage path and DFM report.
    /// Preview URLs may still be pending.
    /// </summary>
    Task SetAnalysisCompletedAsync(string uploadId, string? glbStoragePath = null, object? dfmReport = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the preview URLs as completed (independent of geometry analysis status).
    /// </summary>
    Task SetPreviewUrlsCompletedAsync(string uploadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the preview URLs as failed. Geometry analysis succeeded; only preview generation failed.
    /// </summary>
    Task SetPreviewUrlsFailedAsync(string uploadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the analysis as failed with an error code.
    /// </summary>
    Task SetAnalysisFailedAsync(string uploadId, string errorCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current analysis status for a file.
    /// </summary>
    Task<FileAnalysisStatusDto?> GetStatusAsync(string uploadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrates all cached file-analysis entries from a temp-bucket storage path prefix
    /// to a customer-bucket prefix. This is called by the BFF after
    /// UploadServiceClient.MigrateProjectAsync successfully migrates the
    /// original file, so that viewer and thumbnail URLs derived from the analysis cache
    /// continue to resolve correctly under the new path.
    /// </summary>
    /// <param name="oldStoragePath">The original temp-bucket path prefix (e.g. "projects/{guid}/").</param>
    /// <param name="newStoragePath">The new customer-bucket path prefix (e.g. "customers/{guid}/projects/{guid}/").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    // TODO: Remove NoWarn 1591 suppression from Directory.Build.props once all public members have XML docs.
    Task MigrateGlbStoragePathAsync(string oldStoragePath, string newStoragePath, CancellationToken cancellationToken = default);
}
