using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for delivery note operations, proxying to the Delivery Service.
/// </summary>
/// <param name="client">The delivery service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DeliveryNotesController(IDeliveryServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of delivery notes.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<DeliveryNoteSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await client.GetDeliveryNotesAsync(page, pageSize, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<DeliveryNoteSummaryDto>());
    }

    /// <summary>
    /// Retrieves a delivery note by ID.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id}")]
    public async Task<ActionResult<DeliveryNoteDetailDto>> GetById(string id, CancellationToken ct = default)
    {
        var result = await client.GetDeliveryNoteAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.Create, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<DeliveryNoteDetailDto>> Create([FromBody] CreateDeliveryNoteRequest request, CancellationToken ct)
    {
        var result = await client.CreateDeliveryNoteAsync(request, ct);
        return result != null ? CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1.0" }, result) : BadRequest();
    }

    /// <summary>
    /// Updates the status of a delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.UpdateStatus, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<DeliveryNoteDetailDto>> UpdateStatus(string id, [FromBody] UpdateDeliveryStatusRequest request, CancellationToken ct)
    {
        var result = await client.UpdateDeliveryStatusAsync(id, request, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Generates a PDF for a delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.GeneratePdf, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id}/pdf")]
    public async Task<ActionResult<DeliveryPdfRequestResponse>> GeneratePdf(string id, CancellationToken ct)
    {
        var url = await client.GeneratePdfAsync(id, ct);
        return url != null ? Ok(url) : BadRequest();
    }

    /// <summary>
    /// Retrieves files attached to a delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id}/files")]
    public async Task<ActionResult<List<DeliveryNoteFileDto>>> GetFiles(string id, CancellationToken ct)
    {
        var result = await client.GetFilesAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Downloads a file attached to a delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id}/files/{fileId:guid}/download")]
    public async Task<IActionResult> DownloadFile(string id, Guid fileId, CancellationToken ct)
    {
        using var response = await client.DownloadFileAsync(id, fileId, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "delivery-note-file";

        return File(bytes, contentType, fileName);
    }

    /// <summary>
    /// Uploads a proof or document file to a delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.UpdateStatus, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id}/files")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<DeliveryNoteFileDto>> UploadFile(
        string id,
        IFormFile file,
        [FromForm] string fileType,
        [FromForm] string? description,
        CancellationToken ct)
    {
        if (file.Length == 0)
        {
            return BadRequest();
        }

        await using var stream = file.OpenReadStream();
        var result = await client.UploadFileAsync(
            id,
            stream,
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            fileType,
            description,
            ct);

        return result != null ? CreatedAtAction(nameof(GetFiles), new { id, version = "1.0" }, result) : BadRequest();
    }

    /// <summary>
    /// Deletes a delivery note.
    /// </summary>
    [RequirePermission(MalievPermissions.Delivery.Delete, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken ct)
    {
        var success = await client.DeleteDeliveryNoteAsync(id, ct);
        return success ? NoContent() : NotFound();
    }
}
