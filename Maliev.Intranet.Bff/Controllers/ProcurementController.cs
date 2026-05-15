using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for procurement-related operations, proxying to PurchaseOrderService.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProcurementController(IPurchaseOrderServiceClient client, UploadServiceClient? uploadClient = null) : ControllerBase
{
    private const long MaxPurchaseOrderAttachmentBytes = 25 * 1024 * 1024;

    /// <summary>
    /// Retrieves a paged list of purchase orders.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PurchaseOrderDto>>> Get(
        [FromQuery] string? status = null,
        [FromQuery] string? orderType = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] int? orderId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await client.GetPurchaseOrdersAsync(
            status,
            orderType,
            supplierId,
            orderId,
            fromDate,
            toDate,
            sortBy,
            sortDirection,
            page,
            pageSize,
            ct);

        return result is not null
            ? Ok(result)
            : StatusCode(StatusCodes.Status502BadGateway, "Purchase orders could not be loaded.");
    }

    /// <summary>
    /// Retrieves a purchase order by ID.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(int id, CancellationToken ct)
    {
        var result = await client.GetPurchaseOrderByIdAsync(id, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new purchase order.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Create, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var (result, errorContent, statusCode) = await client.CreatePurchaseOrderAsync(request, ct);
        if (result is not null)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        var message = string.IsNullOrWhiteSpace(errorContent)
            ? "Purchase order could not be created. Check the selected supplier, source order, currency, and line item."
            : errorContent;
        return StatusCode(statusCode == 0 ? StatusCodes.Status400BadRequest : statusCode, message);
    }

    /// <summary>
    /// Approves a purchase order.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Approve, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<PurchaseOrderDto>> Approve(int id, CancellationToken ct)
    {
        var result = await client.ApprovePurchaseOrderAsync(id, ct);
        return result is not null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Sends a purchase order to the supplier.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Send, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:int}/send-to-supplier")]
    public async Task<ActionResult<PurchaseOrderDto>> SendToSupplier(int id, CancellationToken ct)
    {
        var result = await client.SendToSupplierAsync(id, ct);
        return result is not null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Marks purchase order goods as received.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Receive, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:int}/receive")]
    public async Task<ActionResult<PurchaseOrderDto>> Receive(
        int id,
        [FromQuery] bool isPartialReceipt = false,
        CancellationToken ct = default)
    {
        var result = await client.ReceivePurchaseOrderAsync(id, isPartialReceipt, ct);
        return result is not null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Cancels a purchase order.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Cancel, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelPurchaseOrderRequest request, CancellationToken ct)
    {
        var (success, errorContent, statusCode) = await client.CancelPurchaseOrderAsync(id, request, ct);
        if (success)
        {
            return NoContent();
        }

        return StatusCode(statusCode == 0 ? StatusCodes.Status400BadRequest : statusCode, string.IsNullOrWhiteSpace(errorContent) ? "Purchase order could not be cancelled." : errorContent);
    }

    /// <summary>
    /// Uploads and links a file to a purchase order.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.FileUpload, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:int}/files")]
    public async Task<ActionResult<PurchaseOrderFileDto>> UploadFile(
        int id,
        IFormFile file,
        [FromQuery] string documentType = "Reference",
        [FromQuery] string? description = null,
        CancellationToken ct = default)
    {
        if (uploadClient is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "UploadServiceClient is not configured.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (file.Length > MaxPurchaseOrderAttachmentBytes)
        {
            return BadRequest($"File exceeds the {MaxPurchaseOrderAttachmentBytes / 1024 / 1024} MB limit.");
        }

        var safeFileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return BadRequest("File name is required.");
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        var storagePath = $"purchase-orders/{id}/{uniquePrefix}_{safeFileName}";

        using var stream = file.OpenReadStream();
        var upload = await uploadClient.UploadFileAsync(safeFileName, stream, contentType, storagePath, true, ct);
        if (upload is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Upload failed.");
        }

        var (result, errorContent, statusCode) = await client.RegisterFileAsync(id, new RegisterPurchaseOrderFileRequest
        {
            FileName = safeFileName,
            ObjectName = upload.StoragePath ?? upload.FileReference ?? storagePath,
            FileSize = upload.FileSize,
            ContentType = contentType,
            DocumentType = documentType,
            Description = description
        }, ct);

        return result is not null
            ? Ok(result)
            : StatusCode(statusCode == 0 ? StatusCodes.Status502BadGateway : statusCode, string.IsNullOrWhiteSpace(errorContent) ? "Purchase order file could not be linked." : errorContent);
    }

    /// <summary>
    /// Exports a purchase order in the requested format.
    /// </summary>
    [RequirePermission(MalievPermissions.PurchaseOrder.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:int}/export")]
    public async Task<IActionResult> Export(int id, [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        using var response = await client.ExportPurchaseOrderAsync(id, format, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, body);
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        return Content(body, contentType);
    }
}
