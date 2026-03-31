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
/// <param name="analysisStatusService">Service for file analysis status.</param>
/// <param name="fileTypes">File type configuration from appsettings.</param>
[ApiController]
[Route("api/uploads")]
public class UploadsController(
    UploadServiceClient uploadClient,
    IFileAnalysisStatusService analysisStatusService,
    Maliev.Intranet.Client.Services.FileTypesSettings fileTypes) : ControllerBase
{
    /// <summary>
    /// Uploads a single project file to GCS via UploadService.
    /// The file is stored at <c>projects/{projectId}/{fileName}</c>.
    /// </summary>
    /// <param name="file">The multipart file to upload.</param>
    /// <param name="projectId">
    /// The project (or temporary client-side) GUID that scopes the storage path.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="BffUploadResponse"/> containing <c>uploadId</c>, <c>fileName</c>,
    /// <c>fileSize</c>, and <c>storagePath</c> on success.
    /// </returns>
    [HttpPost]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<BffUploadResponse>> UploadAsync(
        IFormFile file,
        [FromQuery] Guid projectId,
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
        // Unique prefix ensures the same filename can be uploaded multiple times
        // (e.g. same model with different process/material configurations).
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        var path = $"projects/{projectId}/{uniquePrefix}_{file.FileName}";

        var result = await uploadClient.UploadFileAsync(file.FileName, stream, contentType, path, true, ct);
        return result != null ? Ok(result) : StatusCode(500, "Upload failed.");
    }

    /// <summary>
    /// Gets the analysis status for an uploaded file by its GCS storage path.
    /// Used by clients to poll for geometry analysis completion.
    /// </summary>
    /// <param name="storagePath">The GCS storage path (e.g., "projects/{guid}/filename.stp").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current analysis status, or 404 if not found.</returns>
    [HttpGet("analysis-status")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<FileAnalysisStatusDto>> GetAnalysisStatusAsync(
        [FromQuery] string storagePath,
        CancellationToken ct)
    {
        var status = await analysisStatusService.GetStatusAsync(storagePath, ct);
        return status != null ? Ok(status) : NotFound();
    }

    /// <summary>
    /// Gets the BabylonJS GLB viewer URL for an uploaded file by its GCS storage path.
    /// The path is used as the cache key by the <see cref="IFileAnalysisStatusService"/>.
    /// Returns 404 when analysis is not yet complete or the GLB artifact is not available.
    /// </summary>
    /// <param name="storagePath">The GCS storage path of the original uploaded file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An object with a <c>url</c> property containing the signed GLB URL, or 404.</returns>
    [HttpGet("viewer-url")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult> GetViewerUrlAsync([FromQuery] string storagePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return BadRequest("storagePath is required.");

        var status = await analysisStatusService.GetStatusAsync(storagePath, ct);

        // Derive GLB path from convention when cache is cold/expired
        var glbPath = status?.GlbStoragePath;
        if (string.IsNullOrEmpty(glbPath))
            glbPath = $"{storagePath}_viewer.glb";

        var signedUrl = await uploadClient.GetDownloadUrlByPathAsync(glbPath, ct);
        // Note: GetDownloadUrlByPathAsync returns null when UploadService returns 401/403
        // (e.g. user lacks FilesDownload permission). The BFF's UserContextHandler logs
        // these as structured errors. Here we surface it as a 404 to avoid leaking
        // permission state to the client.
        if (string.IsNullOrEmpty(signedUrl))
            return NotFound("Viewer artifacts not available yet. The file may still be processing or geometry analysis failed.");
        return Ok(new { Url = signedUrl });
    }

    /// <summary>
    /// Returns a short-lived signed URL for a preview image by its GCS storage path.
    /// Used by the client when preview images are stored as raw storage paths
    /// (signed URL generation failed at consumer time).
    /// </summary>
    /// <param name="storagePath">The GCS storage path of the preview image.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An object with a <c>url</c> property containing the signed URL, or 404.</returns>
    [HttpGet("preview-url")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
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
    /// Files are stored at <c>projects/{projectId}/parts/{partId}/{kind}/{fileName}</c>.
    /// Only the file extensions listed for each kind are accepted.
    /// </summary>
    /// <param name="files">The files to upload.</param>
    /// <param name="projectId">The project GUID.</param>
    /// <param name="partId">The parent part FileId.</param>
    /// <param name="kind">The attachment kind: "Drawing" or "Supplementary".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of attachment DTOs with FileId, StoragePath, Name, FileType, and FileSizeBytes.</returns>
    [HttpPost("attachments")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<List<DraftProjectAttachmentDto>>> UploadAttachmentsAsync(
        List<IFormFile> files,
        [FromQuery] Guid projectId,
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

            var storagePath = $"projects/{projectId}/parts/{partId}/{kind}/{file.FileName}";
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
            });
        }

        return Ok(results);
    }

    /// <summary>
    /// Deletes a drawing or supplementary attachment by its FileId.
    /// </summary>
    /// <param name="fileId">The attachment FileId.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("attachments/{fileId:guid}")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
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
    /// Gets a short-lived (60 min) signed GCS download URL for an uploaded file by its ID.
    /// Used by the BabylonJS viewer to stream the original 3D file directly from GCS.
    /// </summary>
    /// <param name="fileId">The uploaded file GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An object with a <c>url</c> property containing the signed URL, or 404.</returns>
    [HttpGet("{fileId:guid}/download-url")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult> GetDownloadUrlAsync(Guid fileId, CancellationToken ct)
    {
        var signedUrl = await uploadClient.GetDownloadUrlAsync(fileId.ToString(), ct);
        if (string.IsNullOrEmpty(signedUrl)) return NotFound("Download URL not available");
        return Ok(new { Url = signedUrl });
    }

    private static string GetMimeTypeFromExtension(string? extension)
    {
        return extension?.ToLowerInvariant() switch
        {
            // 3D manufacturing
            ".stl"  => "model/stl",
            ".obj"  => "model/obj",
            ".3mf"  => "model/3mf",
            ".step" => "application/step",
            ".stp"  => "application/step",
            ".stpz" => "application/step",
            ".igs"  => "application/iges",
            ".iges" => "application/iges",
            // 3D design
            ".blend" => "application/x-blender",
            ".fbx"   => "application/x-fbx",
            ".gltf"  => "model/gltf+json",
            ".glb"   => "model/gltf-binary",
            // Drawings
            ".pdf"  => "application/pdf",
            ".dxf"  => "application/dxf",
            ".dwg"  => "image/vnd.dwg",
            // Images
            ".png"  => "image/png",
            ".jpg"  => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".tiff" => "image/tiff",
            ".bmp"  => "image/bmp",
            ".webp" => "image/webp",
            // Documents
            ".doc"  => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls"  => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            // Archives
            ".zip"  => "application/zip",
            ".rar"  => "application/vnd.rar",
            ".7z"   => "application/x-7z-compressed",
            _       => "application/octet-stream"
        };
    }
}
