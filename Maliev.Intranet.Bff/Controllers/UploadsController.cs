using System.Net.Http.Headers;
using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// BFF controller for proxying file uploads to the UploadService.
/// </summary>
/// <param name="uploadClient">The typed UploadService HTTP client.</param>
/// <param name="httpClientFactory">Factory used for the non-retry streaming upload proxy client.</param>
/// <param name="analysisStatusService">Service for file analysis status.</param>
/// <param name="fileTypes">File type configuration from appsettings.</param>
/// <param name="logger">Logger for diagnostic events.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/uploads")]
public class UploadsController(
    UploadServiceClient uploadClient,
    IHttpClientFactory httpClientFactory,
    IFileAnalysisStatusService analysisStatusService,
    Maliev.Intranet.Client.Services.FileTypesSettings fileTypes,
    ILogger<UploadsController> logger) : ControllerBase
{
    private const string StreamingUploadClientName = "UploadServiceClient.StreamingProxy";
    private static readonly HashSet<string> BrowserViewerSourceExtensions = new(
        [".3mf", ".glb", ".gltf", ".obj", ".stl"],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Uploads a single project file to GCS via UploadService.
    /// If <paramref name="customerId"/> is provided, the file is stored at
    /// <c>customers/{customerId}/projects/{projectId}/{fileName}</c> (routes to
    /// <c>maliev-customers</c> bucket). Otherwise, the file is stored at
    /// <c>projects/{projectId}/{fileName}</c> (routes to <c>maliev-temp</c> bucket),
    /// allowing drafts to be saved before a customer is selected.
    /// </summary>
    /// <param name="file">The multipart file to upload.</param>
    /// <param name="projectId">
    /// The project (or temporary client-side) GUID that scopes the storage path.
    /// </param>
    /// <param name="customerId">
    /// The customer GUID that owns this project. Optional — if not provided,
    /// the file is uploaded to the temp bucket for later migration.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="BffUploadResponse"/> containing <c>uploadId</c>, <c>fileName</c>,
    /// <c>fileSize</c>, and <c>storagePath</c> on success.
    /// </returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<BffUploadResponse>> UploadAsync(
        IFormFile file,
        [FromQuery] Guid projectId,
        [FromQuery] Guid? customerId,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (projectId == Guid.Empty)
            return BadRequest("projectId is required.");

        var contentType = file.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = GetMimeTypeFromExtension(Path.GetExtension(file.FileName));
        }

        using var stream = file.OpenReadStream();
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        var path = customerId.HasValue
            ? $"customers/{customerId}/projects/{projectId}/{uniquePrefix}_{file.FileName}"
            : $"projects/{projectId}/{uniquePrefix}_{file.FileName}";

        var result = await uploadClient.UploadFileAsync(file.FileName, stream, contentType, path, true, ct);
        return result != null ? Ok(result) : StatusCode(500, "Upload failed.");
    }

    /// <summary>
    /// Uploads multiple project files in a single request, performing the permission check once.
    /// Each file is forwarded to the UploadService individually, but the authorization overhead
    /// is amortized across all files in the batch.
    /// </summary>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("batch")]
    public async Task<ActionResult<List<BffUploadResponse>>> UploadBatchAsync(
        List<IFormFile> files,
        [FromQuery] Guid projectId,
        [FromQuery] Guid? customerId,
        CancellationToken ct)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded.");

        if (projectId == Guid.Empty)
            return BadRequest("projectId is required.");

        var results = new List<BffUploadResponse>(files.Count);

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            var contentType = file.ContentType;
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = GetMimeTypeFromExtension(Path.GetExtension(file.FileName));

            using var stream = file.OpenReadStream();
            var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
            var path = customerId.HasValue
                ? $"customers/{customerId}/projects/{projectId}/{uniquePrefix}_{file.FileName}"
                : $"projects/{projectId}/{uniquePrefix}_{file.FileName}";

            var result = await uploadClient.UploadFileAsync(file.FileName, stream, contentType, path, true, ct);
            if (result != null)
                results.Add(result);
        }

        return Ok(results);
    }

    /// <summary>
    /// Initiates a direct browser-to-GCS resumable upload for a project model file.
    /// </summary>
    /// <param name="request">The file metadata used to create the UploadService session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resumable upload session URI and storage metadata.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("resumable")]
    [ProducesResponseType(typeof(BffResumableUploadSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BffResumableUploadSessionResponse>> InitiateResumableUploadAsync(
        [FromBody] BffInitiateResumableUploadRequest request,
        CancellationToken ct)
    {
        if (request.ProjectId == Guid.Empty)
            return BadRequest("projectId is required.");

        if (request.FileSize <= 0)
            return BadRequest("fileSize must be greater than zero.");

        var fileName = Path.GetFileName(request.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("fileName is required.");

        var extension = Path.GetExtension(fileName);
        if (!fileTypes.ThreeDExtensions.Contains(extension))
            return BadRequest($"File type '{extension}' is not allowed for model uploads.");

        var contentType = string.IsNullOrWhiteSpace(request.ContentType)
            ? GetMimeTypeFromExtension(extension)
            : request.ContentType;

        var storagePath = BuildProjectUploadPath(request.ProjectId, request.CustomerId, fileName);
        var metadataTags = BuildBrowserPrimaryUploadMetadata(extension);
        var (session, errorContent, statusCode) = await uploadClient.InitiateResumableUploadWithDiagnosticsAsync(
            fileName,
            contentType,
            request.FileSize,
            storagePath,
            true,
            metadataTags,
            ct);

        return session != null
            ? Ok(session)
            : StatusCode(
                statusCode == 0 ? StatusCodes.Status502BadGateway : statusCode,
                string.IsNullOrWhiteSpace(errorContent) ? "Upload initiation failed." : errorContent);
    }

    /// <summary>
    /// Completes a direct browser-to-GCS resumable upload after the browser sends the file bytes.
    /// </summary>
    /// <param name="uploadId">The UploadService upload session identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The completed upload metadata.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("resumable/{uploadId}/complete")]
    [ProducesResponseType(typeof(BffUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BffUploadResponse>> CompleteResumableUploadAsync(
        [FromRoute] string uploadId,
        CancellationToken ct)
    {
        var result = await uploadClient.CompleteResumableUploadAsync(uploadId, ct);
        return result != null ? Ok(result) : StatusCode(500, "Upload completion failed.");
    }

    /// <summary>
    /// Proxies a raw resumable upload body to UploadService when direct GCS upload is unavailable.
    /// </summary>
    /// <param name="uploadId">The UploadService upload session identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The UploadService resumable upload response.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("resumable/{uploadId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status308PermanentRedirect)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProxyResumableUploadAsync(
        [FromRoute] string uploadId,
        CancellationToken ct)
    {
        var contentRange = Request.Headers.ContentRange.ToString();
        if (string.IsNullOrWhiteSpace(contentRange))
            return BadRequest("Content-Range header is required.");

        if (!ContentRangeHeaderValue.TryParse(contentRange, out _))
            return BadRequest("Content-Range header is invalid.");

        var streamingUploadClient = new UploadServiceClient(StreamingUploadClientName, httpClientFactory);
        using var response = await streamingUploadClient.ResumeResumableUploadAsync(
            uploadId,
            Request.Body,
            Request.ContentType,
            Request.ContentLength,
            contentRange,
            ct);

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        return StatusCode((int)response.StatusCode, responseBody);
    }

    /// <summary>
    /// Gets the analysis status for an uploaded file by its GCS storage path.
    /// Used by clients to poll for geometry analysis completion.
    /// </summary>
    /// <param name="storagePath">The GCS storage path (e.g., "projects/{guid}/filename.stp").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current analysis status, or 404 if not found.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("analysis-status")]
    public async Task<ActionResult<FileAnalysisStatusDto>> GetAnalysisStatusAsync(
        [FromQuery] string storagePath,
        CancellationToken ct)
    {
        var status = await analysisStatusService.GetStatusAsync(storagePath, ct);
        if (status != null)
            return Ok(status);

        var directBrowserViewerArtifact = await TryRecoverDirectBrowserViewerArtifactAsync(storagePath, ct);
        if (directBrowserViewerArtifact != null)
        {
            return Ok(new FileAnalysisStatusDto
            {
                UploadId = storagePath,
                Status = FileAnalysisStatus.Completed,
                GlbStoragePath = null,
                ViewerStoragePath = directBrowserViewerArtifact.StoragePath,
                ViewerFileExtension = directBrowserViewerArtifact.FileExtension,
                GlbSignedUrl = directBrowserViewerArtifact.SignedUrl,
                PreviewProcessingStatus = PreviewProcessingStatus.Completed,
                ProcessedAt = DateTimeOffset.UtcNow
            });
        }

        var recoveredViewerArtifact = await TryRecoverConventionalViewerGlbAsync(storagePath, ct);
        return recoveredViewerArtifact != null
            ? Ok(new FileAnalysisStatusDto
            {
                UploadId = storagePath,
                Status = FileAnalysisStatus.Completed,
                GlbStoragePath = recoveredViewerArtifact.StoragePath,
                ViewerStoragePath = recoveredViewerArtifact.StoragePath,
                ViewerFileExtension = recoveredViewerArtifact.FileExtension,
                GlbSignedUrl = recoveredViewerArtifact.SignedUrl,
                PreviewProcessingStatus = PreviewProcessingStatus.Completed,
                ProcessedAt = DateTimeOffset.UtcNow
            })
            : NotFound();
    }

    /// <summary>
    /// Gets the BabylonJS GLB viewer URL for an uploaded file by its GCS storage path.
    /// The path is used as the cache key by the <see cref="IFileAnalysisStatusService"/>.
    /// Returns 404 when analysis is not yet complete or the GLB artifact is not available.
    /// </summary>
    /// <param name="storagePath">The GCS storage path of the original uploaded file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An object with a <c>url</c> property containing the signed GLB URL, or 404.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("viewer-url")]
    public async Task<ActionResult> GetViewerUrlAsync([FromQuery] string storagePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return BadRequest("storagePath is required.");

        var status = await analysisStatusService.GetStatusAsync(storagePath, ct);

        // Primary path: resolve the browser viewer source via the analysis status cache.
        // For browser-loadable mesh uploads this can be the original StoragePath; for
        // STEP/IGES it remains the generated GLB artifact path.
        var viewerPath = status?.ViewerStoragePath ?? status?.GlbStoragePath;
        if (!string.IsNullOrEmpty(viewerPath))
        {
            var signedUrl = await uploadClient.GetDownloadUrlByPathAsync(viewerPath, ct);
            if (!string.IsNullOrEmpty(signedUrl))
                return Ok(new
                {
                    Url = signedUrl,
                    ViewerStoragePath = viewerPath,
                    ViewerFileExtension = NormalizeViewerFileExtension(
                        status?.ViewerFileExtension,
                        viewerPath)
                });

            return NotFound("Viewer artifact not available. The file may still be processing or geometry analysis failed.");
        }

        var directBrowserViewerArtifact = await TryRecoverDirectBrowserViewerArtifactAsync(storagePath, ct);
        if (directBrowserViewerArtifact != null)
        {
            return Ok(new
            {
                Url = directBrowserViewerArtifact.SignedUrl,
                ViewerStoragePath = directBrowserViewerArtifact.StoragePath,
                ViewerFileExtension = directBrowserViewerArtifact.FileExtension
            });
        }

        // Fallback: for overlay GLB files (e.g. "_FDM__thin_wall_overlay.glb") and other
        // artifacts that are stored directly in GCS but never tracked in the analysis cache,
        // try signing the requested storagePath directly. This is safe because
        // GetDownloadUrlByPathAsync returns null when the object doesn't exist in GCS,
        // preventing speculative signed URLs for non-existent files.
        if (storagePath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
        {
            var directUrl = await uploadClient.GetDownloadUrlByPathAsync(storagePath, ct);
            if (!string.IsNullOrEmpty(directUrl))
                return Ok(new
                {
                    Url = directUrl,
                    ViewerStoragePath = storagePath,
                    ViewerFileExtension = ".glb"
                });

            return NotFound("Viewer artifact not found in storage.");
        }

        var recoveredViewerArtifact = await TryRecoverConventionalViewerGlbAsync(storagePath, ct);
        if (recoveredViewerArtifact != null)
        {
            return Ok(new
            {
                Url = recoveredViewerArtifact.SignedUrl,
                ViewerStoragePath = recoveredViewerArtifact.StoragePath,
                ViewerFileExtension = recoveredViewerArtifact.FileExtension
            });
        }

        // Not a GLB path and no cache entry — the file is still processing.
        return NotFound("Viewer artifact not available. The file may still be processing.");
    }

    private async Task<RecoveredViewerArtifact?> TryRecoverDirectBrowserViewerArtifactAsync(
        string storagePath,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return null;

        var extension = Path.GetExtension(storagePath);
        if (!BrowserViewerSourceExtensions.Contains(extension))
            return null;

        string? signedUrl;
        try
        {
            signedUrl = await uploadClient.GetDownloadUrlByPathAsync(storagePath, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not sign direct browser viewer source for {StoragePath}.", storagePath);
            return null;
        }

        return string.IsNullOrEmpty(signedUrl)
            ? null
            : new RecoveredViewerArtifact(storagePath, signedUrl, NormalizeViewerFileExtension(null, storagePath) ?? extension);
    }

    private async Task<RecoveredViewerArtifact?> TryRecoverConventionalViewerGlbAsync(
        string storagePath,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storagePath)
            || storagePath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var extension = Path.GetExtension(storagePath);
        if (string.IsNullOrWhiteSpace(extension) || !fileTypes.ThreeDExtensions.Contains(extension))
            return null;

        var glbStoragePath = storagePath + "_viewer.glb";
        string? signedUrl;
        try
        {
            signedUrl = await uploadClient.GetDownloadUrlByPathAsync(glbStoragePath, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not probe conventional GLB viewer artifact for {StoragePath}.", storagePath);
            return null;
        }

        return string.IsNullOrEmpty(signedUrl)
            ? null
            : new RecoveredViewerArtifact(glbStoragePath, signedUrl, ".glb");
    }

    private static string? NormalizeViewerFileExtension(string? fileExtension, string? storagePath)
    {
        var ext = !string.IsNullOrWhiteSpace(fileExtension)
            ? fileExtension.Trim()
            : Path.GetExtension(storagePath);

        if (string.IsNullOrWhiteSpace(ext))
            return null;

        return (ext.StartsWith('.') ? ext : "." + ext).ToLowerInvariant();
    }

    /// <summary>
    /// Returns a short-lived signed URL for a preview image by its GCS storage path.
    /// Used by the client when preview images are stored as raw storage paths
    /// (signed URL generation failed at consumer time).
    /// </summary>
    /// <param name="storagePath">The GCS storage path of the preview image.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An object with a <c>url</c> property containing the signed URL, or 404.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("preview-url")]
    public async Task<ActionResult> GetPreviewUrlAsync([FromQuery] string storagePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return BadRequest("storagePath is required.");
        var signedUrl = await uploadClient.GetDownloadUrlByPathAsync(storagePath, ct, expirationMinutes: 10080);
        if (string.IsNullOrEmpty(signedUrl)) return NotFound("Preview URL not available");
        return Ok(new { Url = signedUrl });
    }

    /// <summary>
    /// Uploads one or more drawing or supplementary files attached to a specific part.
    /// If <paramref name="customerId"/> is provided, files are stored at
    /// <c>customers/{customerId}/projects/{projectId}/parts/{partId}/{kind}/</c>.
    /// Otherwise, files are stored at <c>projects/{projectId}/parts/{partId}/{kind}/</c>
    /// (temp bucket). Only the file extensions listed for each kind are accepted.
    /// </summary>
    /// <param name="files">The files to upload.</param>
    /// <param name="projectId">The project GUID.</param>
    /// <param name="customerId">
    /// The customer GUID that owns this project. Optional.
    /// </param>
    /// <param name="partId">The parent part FileId.</param>
    /// <param name="kind">The attachment kind: "Drawing" or "Supplementary".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of attachment DTOs with FileId, StoragePath, Name, FileType, and FileSizeBytes.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("attachments")]
    public async Task<ActionResult<List<DraftProjectAttachmentDto>>> UploadAttachmentsAsync(
        List<IFormFile> files,
        [FromQuery] Guid projectId,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid partId,
        [FromQuery] string kind,
        CancellationToken ct)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded.");

        if (projectId == Guid.Empty)
            return BadRequest("projectId is required.");

        if (partId == Guid.Empty)
            return BadRequest("partId is required.");

        if (string.IsNullOrWhiteSpace(kind) ||
            (kind != "Drawing" && kind != "Supplementary"))
            return BadRequest("kind must be 'Drawing' or 'Supplementary'.");

        var allowed = kind == "Drawing" ? fileTypes.DrawingExtensions : fileTypes.SupplementaryExtensions;

        var results = new List<DraftProjectAttachmentDto>();

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName);
            if (!allowed.Contains(ext))
            {
                return BadRequest($"File type '{ext}' is not allowed for {kind} attachments. Allowed: {string.Join(", ", allowed)}.");
            }

            var contentType = file.ContentType;
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = GetMimeTypeFromExtension(ext) ?? "application/octet-stream";

            var storagePath = customerId.HasValue
                ? $"customers/{customerId}/projects/{projectId}/parts/{partId}/{kind}/{file.FileName}"
                : $"projects/{projectId}/parts/{partId}/{kind}/{file.FileName}";
            using var stream = file.OpenReadStream();
            var uploadResult = await uploadClient.UploadFileAsync(file.FileName, stream, contentType, storagePath, true, ct);

            if (uploadResult == null)
                return StatusCode(500, $"Upload failed for {file.FileName}.");

            results.Add(new DraftProjectAttachmentDto
            {
                FileId = Guid.TryParse(uploadResult.UploadId, out var fid) ? fid : Guid.NewGuid(),
                StoragePath = uploadResult.StoragePath ?? storagePath,
                Name = file.FileName,
                FileType = contentType,
                FileSizeBytes = file.Length,
                Kind = kind == "Drawing" ? DraftAttachmentKind.Drawing : DraftAttachmentKind.Supplementary,
                UploadedAt = DateTime.UtcNow,
            });
        }

        return Ok(results);
    }

    /// <summary>
    /// Deletes a drawing or supplementary attachment by its FileId.
    /// </summary>
    /// <param name="fileId">The attachment FileId.</param>
    /// <param name="ct">Cancellation token.</param>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("attachments/{fileId:guid}")]
    public async Task<IActionResult> DeleteAttachmentAsync(Guid fileId, CancellationToken ct)
    {
        try
        {
            await uploadClient.DeleteFileAsync(fileId, ct);
            return NoContent();
        }
        catch (HttpRequestException ex)
        {
            return NotFound($"Attachment {fileId} not found or could not be deleted: {ex.Message}");
        }
    }

/// <summary>
    /// Returns a short-lived signed GCS download URL for an uploaded file by its ID.
    /// Used by the BabylonJS viewer to stream the original 3D file directly from GCS.
    /// </summary>
    /// <param name="fileId">The uploaded file GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An object with a <c>url</c> property containing the signed URL, or 404.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{fileId:guid}/download-url")]
    public async Task<ActionResult> GetDownloadUrlAsync(Guid fileId, CancellationToken ct)
    {
        var signedUrl = await uploadClient.GetDownloadUrlAsync(fileId.ToString(), ct);
        if (string.IsNullOrEmpty(signedUrl)) return NotFound("Download URL not available");
        return Ok(new { Url = signedUrl });
    }

    /// <summary>
    /// Gets a temporary signed download URL for a file using its GCS storage path.
    /// Used by the frontend for thumbnail generation and direct file access.
    /// </summary>
    /// <param name="request">Request containing the storage path and optional expiration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An object with a <c>signedUrl</c> property containing the signed URL, or 404.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("by-path/signed-url")]
    [ProducesResponseType(typeof(SignedUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetSignedUrlByPathAsync(
        [FromBody] GetSignedUrlByPathRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.StoragePath))
            return BadRequest("storagePath is required.");

        var signedUrl = await uploadClient.GetDownloadUrlByPathAsync(
            request.StoragePath, ct, request.ExpirationMinutes ?? 60);

        if (string.IsNullOrEmpty(signedUrl))
            return NotFound("Signed URL not available for the given storage path.");

        return Ok(new { SignedUrl = signedUrl });
    }

    /// <summary>Request DTO for getting a signed URL by storage path.</summary>
    /// <param name="StoragePath">The GCS storage path of the file.</param>
    /// <param name="ExpirationMinutes">Optional expiration in minutes (default 60).</param>
    public sealed record GetSignedUrlByPathRequest(string StoragePath, int? ExpirationMinutes = 60);

    /// <summary>Response DTO containing a signed URL.</summary>
    /// <param name="SignedUrl">The signed download URL.</param>
    public sealed record SignedUrlResponse(string SignedUrl);

    /// <summary>
    /// Migrates all files for a project from the temp bucket to the customer bucket.
    /// Called by the frontend when a customer is selected for a draft project that
    /// already has uploaded files in the temp bucket.
    /// </summary>
    /// <param name="projectId">The project GUID whose files should be migrated.</param>
    /// <param name="customerId">The target customer GUID.</param>
    /// <param name="dryRun">If true, only reports what would be migrated without making changes.</param>
    /// <param name="ct">Cancellation token.</param>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("migrate-project")]
    [ProducesResponseType(typeof(BffMigrateProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MigrateProjectAsync(
        [FromQuery] Guid projectId,
        [FromQuery] Guid customerId,
        [FromQuery] bool dryRun = false,
        CancellationToken ct = default)
    {
        if (projectId == Guid.Empty)
            return BadRequest("projectId is required.");

        if (customerId == Guid.Empty)
            return BadRequest("customerId is required.");

        var result = await uploadClient.MigrateProjectAsync(projectId, customerId, dryRun, ct);
        if (result == null)
            return StatusCode(500, "Migration failed.");

        var response = new BffMigrateProjectResponseDto
        {
            DryRun = result.DryRun,
            TotalEvaluated = result.TotalEvaluated,
            TotalMigrated = result.TotalMigrated,
            Errors = [.. result.Errors],
        };

        if (!dryRun && result.MigratedFiles.Count > 0)
        {
            foreach (var file in result.MigratedFiles)
            {
                await analysisStatusService.RegisterStoragePathAliasAsync(file.OldPath, file.NewPath, ct);
                var status = await ReconcileMigratedAnalysisStatusAsync(file, ct);
                response.MigratedFiles.Add(new BffMigratedProjectFileDto
                {
                    FileId = file.FileId,
                    OldPath = file.OldPath,
                    NewPath = file.NewPath,
                    Status = status,
                });
            }
        }
        else
        {
            response.MigratedFiles.AddRange(result.MigratedFiles.Select(file => new BffMigratedProjectFileDto
            {
                FileId = file.FileId,
                OldPath = file.OldPath,
                NewPath = file.NewPath,
                Status = null,
            }));
        }

        return Ok(response);
    }

    private async Task<FileAnalysisStatusDto?> ReconcileMigratedAnalysisStatusAsync(
        MigratedFileEntry file,
        CancellationToken ct)
    {
        var oldStatus = await analysisStatusService.GetStatusAsync(file.OldPath, ct);
        if (oldStatus == null)
        {
            logger.LogInformation(
                "No cached analysis status for migrated file {FileId} at {OldPath}; late geometry events will update {NewPath}.",
                file.FileId,
                file.OldPath,
                file.NewPath);
            return await analysisStatusService.GetStatusAsync(file.NewPath, ct);
        }

        var thumbnailSmall = await CopyMigratedArtifactAsync(
            oldStatus.PreviewUrls?.ThumbnailSmallGcsPath,
            file.OldPath,
            file.NewPath,
            oldStatus.PreviewUrls?.ThumbnailSmall,
            expirationMinutes: 10080,
            ct);
        var thumbnailLarge = await CopyMigratedArtifactAsync(
            oldStatus.PreviewUrls?.ThumbnailLargeGcsPath,
            file.OldPath,
            file.NewPath,
            oldStatus.PreviewUrls?.ThumbnailLargeUrl ?? oldStatus.HiResThumbnailUrl,
            expirationMinutes: 10080,
            ct);
        var glb = await CopyMigratedArtifactAsync(
            oldStatus.GlbStoragePath,
            file.OldPath,
            file.NewPath,
            oldStatus.GlbSignedUrl,
            expirationMinutes: 60,
            ct);

        await analysisStatusService.CloneStatusAsync(
            file.OldPath,
            file.NewPath,
            thumbnailSmall.SignedUrl,
            thumbnailLarge.SignedUrl,
            thumbnailSmall.StoragePath,
            thumbnailLarge.StoragePath,
            glb.StoragePath,
            glb.SignedUrl,
            ct);

        var reconciledStatus = await analysisStatusService.GetStatusAsync(file.NewPath, ct);
        if (reconciledStatus == null)
        {
            logger.LogWarning(
                "Analysis status reconciliation for migrated file {FileId} did not produce a status at {NewPath}.",
                file.FileId,
                file.NewPath);
        }

        return reconciledStatus;
    }

    private async Task<MigratedArtifact> CopyMigratedArtifactAsync(
        string? sourcePath,
        string oldBasePath,
        string newBasePath,
        string? fallbackSignedUrl,
        int expirationMinutes,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return new MigratedArtifact(null, fallbackSignedUrl);

        var destinationPath = RewriteMigratedStoragePath(sourcePath, oldBasePath, newBasePath);
        if (string.Equals(destinationPath, sourcePath, StringComparison.OrdinalIgnoreCase))
            return new MigratedArtifact(sourcePath, fallbackSignedUrl);

        var copied = await uploadClient.CopyFileAsync(sourcePath, destinationPath, ct);
        if (!copied)
        {
            logger.LogWarning(
                "Could not copy migrated artifact {SourcePath} to {DestinationPath}; preserving existing signed URL fallback.",
                sourcePath,
                destinationPath);
            return new MigratedArtifact(sourcePath, fallbackSignedUrl);
        }

        var signedUrl = await uploadClient.GetDownloadUrlByPathAsync(destinationPath, ct, expirationMinutes);
        return new MigratedArtifact(destinationPath, signedUrl ?? fallbackSignedUrl);
    }

    private static string RewriteMigratedStoragePath(string storagePath, string oldBasePath, string newBasePath)
    {
        if (!storagePath.StartsWith(oldBasePath, StringComparison.OrdinalIgnoreCase))
            return storagePath;

        return newBasePath + storagePath[oldBasePath.Length..];
    }

    private readonly record struct MigratedArtifact(string? StoragePath, string? SignedUrl);

    private sealed record RecoveredViewerArtifact(
        string StoragePath,
        string SignedUrl,
        string FileExtension);

    private static string GetMimeTypeFromExtension(string? extension)
    {
        return extension?.ToLowerInvariant() switch
        {
            // 3D manufacturing
            ".stl" => "model/stl",
            ".obj" => "model/obj",
            ".3mf" => "model/3mf",
            ".step" => "application/step",
            ".stp" => "application/step",
            ".stpz" => "application/step",
            ".igs" => "application/iges",
            ".iges" => "application/iges",
            // 3D design
            ".blend" => "application/x-blender",
            ".fbx" => "application/x-fbx",
            ".gltf" => "model/gltf+json",
            ".glb" => "model/gltf-binary",
            // Drawings
            ".pdf" => "application/pdf",
            ".dxf" => "application/dxf",
            ".dwg" => "image/vnd.dwg",
            // Images
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".tiff" => "image/tiff",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            // Documents
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            // Archives
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",
            _ => "application/octet-stream"
        };
    }

    private static IReadOnlyDictionary<string, string>? BuildBrowserPrimaryUploadMetadata(string extension)
    {
        if (!BrowserViewerSourceExtensions.Contains(extension))
            return null;

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["geometry.executionPolicy"] = "browser_primary",
            ["geometry.browserRuntime"] = "required",
            ["geometry.serverGlbExport"] = "skip_for_browser_viewable"
        };
    }

    private static string BuildProjectUploadPath(Guid projectId, Guid? customerId, string fileName)
    {
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        return customerId.HasValue
            ? $"customers/{customerId}/projects/{projectId}/{uniquePrefix}_{fileName}"
            : $"projects/{projectId}/{uniquePrefix}_{fileName}";
    }
}
