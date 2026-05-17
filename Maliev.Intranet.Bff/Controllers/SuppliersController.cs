using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for supplier-related operations, proxying to the Supplier Service.
/// </summary>
/// <param name="client">The supplier service client.</param>
/// <param name="registryClient">The registry service client.</param>
/// <param name="uploadClient">The upload service client.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SuppliersController(
    SupplierServiceClient client,
    RegistryServiceClient registryClient,
    UploadServiceClient uploadClient,
    ILogger<SuppliersController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of suppliers.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of suppliers.</returns>
    [RequirePermission(MalievPermissions.Supplier.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<SupplierSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetSuppliersAsync(page, pageSize);
        return result != null ? Ok(result) : Ok(new PagedResponse<SupplierSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single supplier.
    /// </summary>
    /// <param name="id">The supplier ID.</param>
    /// <returns>The supplier details.</returns>
    [RequirePermission(MalievPermissions.Supplier.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDetailDto>> GetById(Guid id)
    {
        var result = await client.GetSupplierByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Searches Thai company registry records for supplier onboarding.
    /// </summary>
    [RequirePermission(MalievPermissions.Supplier.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("companies/search")]
    public async Task<ActionResult<IReadOnlyList<RegistryCompanyProfile>>> SearchRegistryCompanies(
        [FromQuery] string query,
        [FromQuery] int limit = 8,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Ok(Array.Empty<RegistryCompanyProfile>());
        }

        limit = Math.Clamp(limit, 1, 20);
        var result = await registryClient.SearchCompaniesAsync(query.Trim(), limit, ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new supplier.
    /// </summary>
    [RequirePermission(MalievPermissions.Supplier.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        using var response = await client.CreateSupplierAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            return string.IsNullOrWhiteSpace(body)
                ? StatusCode((int)response.StatusCode)
                : StatusCode((int)response.StatusCode, body);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return StatusCode((int)response.StatusCode);
        }

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = body,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }

    /// <summary>
    /// Updates an existing supplier.
    /// </summary>
    [RequirePermission(MalievPermissions.Supplier.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
    {
        var response = await client.UpdateSupplierAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Deactivates a supplier.
    /// </summary>
    [RequirePermission(MalievPermissions.Supplier.Delete, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var response = await client.DeactivateSupplierAsync(id, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Uploads and links supplier document lifecycle metadata.
    /// </summary>
    [RequirePermission(MalievPermissions.Supplier.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> AddDocument(
        Guid id,
        [FromForm] IFormFile? file,
        [FromForm] string documentType,
        [FromForm] string documentName,
        [FromForm] DateOnly? issueDate,
        [FromForm] DateOnly? expirationDate,
        [FromForm] string? notes,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(documentName))
        {
            return BadRequest("Document type and name are required.");
        }

        if (file?.Length > 10 * 1024 * 1024)
        {
            return BadRequest("Supplier document exceeds the 10 MB limit.");
        }

        string? fileReference = null;
        if (file is { Length: > 0 })
        {
            var safeFileName = Path.GetFileName(file.FileName);
            var safeDocumentType = documentType.Trim().Replace(' ', '-');
            var path = $"supplier-documents/{id}/{safeDocumentType}/{safeFileName}";
            await using var stream = file.OpenReadStream();
            var upload = await uploadClient.UploadFileAsync(
                safeFileName,
                stream,
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                path,
                overwrite: true,
                ct);

            if (upload == null)
            {
                logger.LogWarning("Supplier document upload failed for supplier {SupplierId} and file {FileName}", id, safeFileName);
                return StatusCode(StatusCodes.Status502BadGateway, "Supplier document upload failed.");
            }

            fileReference = upload.StoragePath ?? upload.FileReference ?? upload.UploadId;
        }

        var request = new CreateSupplierDocumentRequest
        {
            DocumentType = documentType.Trim(),
            DocumentName = documentName.Trim(),
            IssueDate = issueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            ExpirationDate = expirationDate,
            ExternalFileRef = fileReference,
            Notes = notes?.Trim()
        };

        using var response = await client.AddSupplierDocumentAsync(id, request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            return string.IsNullOrWhiteSpace(body)
                ? StatusCode((int)response.StatusCode)
                : StatusCode((int)response.StatusCode, body);
        }

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = body,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }

    /// <summary>
    /// Deletes supplier document lifecycle metadata.
    /// </summary>
    [RequirePermission(MalievPermissions.Supplier.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        var response = await client.DeleteSupplierDocumentAsync(id, documentId, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Gets a signed URL for a supplier document file reference.
    /// </summary>
    [RequirePermission(MalievPermissions.Supplier.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}/documents/download-url")]
    public async Task<IActionResult> GetDocumentDownloadUrl(
        Guid id,
        [FromQuery] string fileReference,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileReference))
        {
            return BadRequest("File reference is required.");
        }

        var trimmedReference = fileReference.Trim();
        var url = LooksLikeStoragePath(trimmedReference)
            ? await uploadClient.GetDownloadUrlByPathAsync(trimmedReference, ct)
            : await uploadClient.GetDownloadUrlAsync(trimmedReference, ct);

        return string.IsNullOrWhiteSpace(url)
            ? NotFound("Supplier document file was not found.")
            : Ok(new { url });
    }

    private static bool LooksLikeStoragePath(string fileReference) =>
        fileReference.Contains('/', StringComparison.Ordinal) || fileReference.Contains('\\', StringComparison.Ordinal);
}
