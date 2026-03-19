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

        var result = await uploadClient.UploadFileAsync(file.FileName, stream, contentType, path, ct);
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
