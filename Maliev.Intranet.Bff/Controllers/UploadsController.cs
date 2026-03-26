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
[ApiController]
[Route("api/uploads")]
public class UploadsController(
    UploadServiceClient uploadClient,
    IFileAnalysisStatusService analysisStatusService) : ControllerBase
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
        var path = $"projects/{projectId}/{file.FileName}";

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
        var signedUrl = await uploadClient.GetDownloadUrlByPathAsync(storagePath, ct);
        if (string.IsNullOrEmpty(signedUrl)) return NotFound("Preview URL not available");
        return Ok(new { Url = signedUrl });
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
