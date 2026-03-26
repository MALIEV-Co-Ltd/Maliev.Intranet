using Asp.Versioning;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for managing 3D models.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/models")]
public class ModelsController(UploadServiceClient uploadClient) : ControllerBase
{
    /// <summary>
    /// Lists 3D models.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<Model3DDto>>> GetModels([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? filter = null, CancellationToken ct = default)
    {
        var result = await uploadClient.GetFilesAsync(page, pageSize, filter, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<Model3DDto>());
    }

    /// <summary>
    /// Gets a model by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Model3DDto>> GetModel(Guid id, CancellationToken ct)
    {
        var result = await uploadClient.GetFileByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Uploads a 3D model.
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<BffUploadResponse>> UploadModel(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        using var stream = file.OpenReadStream();
        // Path starts with 'models/' to trigger GeometryService analysis in UploadService
        var path = $"models/{file.FileName}";

        var result = await uploadClient.UploadFileAsync(file.FileName, stream, file.ContentType, path, true, ct);
        if (result == null) return StatusCode(500, "Failed to upload model.");

        return Ok(result);
    }

    /// <summary>
    /// Gets a signed viewer URL (GLB).
    /// </summary>
    [HttpGet("{id:guid}/viewer-url")]
    public async Task<ActionResult<string>> GetViewerUrl(Guid id, CancellationToken ct)
    {
        var model = await uploadClient.GetFileByIdAsync(id, ct);
        if (model == null) return NotFound();

        // If GLB path is stored in metadata (Model3DDto should have it if we added it to UploadService)
        // Currently UploadServiceClient.GetFileByIdAsync returns Model3DDto which I just added.
        // But UploadService needs to populate ViewerUrl/ThumbnailUrl.
        // Assuming Model3DDto.ViewerUrl is already a signed URL returned by UploadService.

        if (!string.IsNullOrEmpty(model.ViewerUrl))
        {
            return Ok(new { Url = model.ViewerUrl });
        }

        // Fallback: If UploadService doesn't return pre-signed URLs, we might need to ask for one using a reference.
        // This depends on UploadService implementation. For now assume Model3DDto contains them or we construct them.

        return NotFound("Viewer artifacts not available");
    }

    /// <summary>
    /// Gets a signed thumbnail URL.
    /// </summary>
    [HttpGet("{id:guid}/thumbnail-url")]
    public async Task<ActionResult<string>> GetThumbnailUrl(Guid id, CancellationToken ct)
    {
        var model = await uploadClient.GetFileByIdAsync(id, ct);
        if (model == null) return NotFound();

        if (!string.IsNullOrEmpty(model.ThumbnailUrl))
        {
            return Ok(new { Url = model.ThumbnailUrl });
        }

        return NotFound("Thumbnail not available");
    }

    /// <summary>
    /// Deletes a model.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteModel(Guid id, CancellationToken ct)
    {
        await uploadClient.DeleteFileAsync(id, ct);
        return NoContent();
    }
}
