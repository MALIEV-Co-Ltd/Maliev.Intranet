using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Employee Commerce catalog management endpoints.
/// </summary>
/// <param name="client">Commerce service client.</param>
/// <param name="uploadClient">Upload service client for product and collection media assets.</param>
/// <param name="pdfClient">PDF service client.</param>
/// <param name="httpClientFactory">HTTP client factory for generated PDF downloads.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/commerce")]
public sealed class CommerceController(
    CommerceServiceClient client,
    UploadServiceClient uploadClient,
    PdfServiceClient? pdfClient = null,
    IHttpClientFactory? httpClientFactory = null) : ControllerBase
{
    private const long MaxProductMediaBytes = 10 * 1024 * 1024;
    private const long MaxProductDocumentBytes = 20 * 1024 * 1024;
    private static readonly HashSet<string> ProductMediaExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly HashSet<string> ProductDocumentExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };

    /// <summary>
    /// Lists products, including drafts and archived listings.
    /// </summary>
    [HttpGet("products")]
    [RequirePermission(MalievPermissions.Commerce.ProductsRead, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<PagedResponse<CommerceProductSummaryDto>>> ListProducts(
        [FromQuery] string? query,
        [FromQuery] string? collection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var result = await client.ListManagedProductsAsync(query, collection, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a managed product by handle.
    /// </summary>
    [HttpGet("products/{handle}")]
    [RequirePermission(MalievPermissions.Commerce.ProductsRead, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<CommerceProductDto>> GetProduct(string handle, CancellationToken cancellationToken)
    {
        var result = await client.GetManagedProductAsync(handle, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Generates and downloads a product bill of materials PDF.
    /// </summary>
    [HttpGet("products/{handle}/bom/pdf")]
    [RequirePermission(MalievPermissions.Commerce.ProductsRead, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> DownloadProductBomPdf(string handle, CancellationToken cancellationToken)
    {
        if (pdfClient is null || httpClientFactory is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "PDF export is not configured.");
        }

        var product = await client.GetManagedProductAsync(handle, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        if (product.BomItems.Count == 0)
        {
            return BadRequest("Product does not have BOM items to export.");
        }

        var pdfData = BuildCommerceBomPdfData(product);
        var pdfUrl = await pdfClient.GeneratePdfAsync(
            PdfDocumentType.CommerceBom,
            $"commerce-bom-{product.Handle}",
            pdfData,
            ct: cancellationToken);

        if (string.IsNullOrWhiteSpace(pdfUrl))
        {
            return StatusCode(StatusCodes.Status502BadGateway, "BOM PDF could not be generated.");
        }

        using var response = await httpClientFactory.CreateClient().GetAsync(pdfUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Generated BOM PDF could not be downloaded.");
        }

        var pdfBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return File(pdfBytes, "application/pdf", BuildBomDownloadFileName(product.Handle));
    }

    /// <summary>
    /// Creates a product listing.
    /// </summary>
    [HttpPost("products")]
    [RequirePermission(MalievPermissions.Commerce.ProductsCreate, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<CommerceProductDto>> CreateProduct([FromBody] CommerceProductMutationRequest request, CancellationToken cancellationToken)
    {
        var result = await client.CreateProductAsync(request, cancellationToken);
        return result is null ? BadRequest() : CreatedAtAction(nameof(GetProduct), new { handle = result.Handle }, result);
    }

    /// <summary>
    /// Updates a product listing.
    /// </summary>
    [HttpPatch("products/{id:guid}")]
    [RequirePermission(MalievPermissions.Commerce.ProductsUpdate, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<CommerceProductDto>> UpdateProduct(Guid id, [FromBody] CommerceProductMutationRequest request, CancellationToken cancellationToken)
    {
        var result = await client.UpdateProductAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Updates product status.
    /// </summary>
    [HttpPatch("products/{id:guid}/status")]
    [RequirePermission(MalievPermissions.Commerce.ProductsUpdate, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<CommerceProductDto>> UpdateProductStatus(Guid id, [FromBody] CommerceProductStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await client.UpdateProductStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Uploads product media images to central storage and returns stable media references.
    /// </summary>
    [HttpPost("products/media")]
    [RequirePermission(MalievPermissions.Commerce.ProductsUpdate, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<List<BffUploadResponse>>> UploadProductMedia(
        [FromForm] List<IFormFile> files,
        [FromQuery] string? handle,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return BadRequest("No image files uploaded.");
        }

        var safeHandle = BuildSafeMediaHandle(handle);
        var results = new List<BffUploadResponse>(files.Count);

        foreach (var file in files)
        {
            if (file.Length is <= 0 or > MaxProductMediaBytes)
            {
                return BadRequest($"{file.FileName} must be between 1 byte and 10 MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!ProductMediaExtensions.Contains(extension))
            {
                return BadRequest($"{file.FileName} must be a JPG, PNG, or WEBP image.");
            }

            var safeFileName = BuildSafeMediaFileName(file.FileName);
            var storagePath = $"commerce/products/{safeHandle}/media/{Guid.NewGuid():N}_{safeFileName}";
            var contentType = GetProductMediaContentType(file, extension);

            await using var stream = file.OpenReadStream();
            var upload = await uploadClient.UploadFileAsync(safeFileName, stream, contentType, storagePath, true, cancellationToken);
            if (upload is null)
            {
                return StatusCode(StatusCodes.Status502BadGateway, $"Upload failed for {file.FileName}.");
            }

            upload.FileReference = BuildProductMediaReference(upload.UploadId);
            results.Add(upload);
        }

        return Ok(results);
    }

    /// <summary>
    /// Uploads a BOM item document (image or PDF) and returns a stable media reference.
    /// </summary>
    [HttpPost("products/documents")]
    [RequirePermission(MalievPermissions.Commerce.ProductsUpdate, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<BffUploadResponse>> UploadProductDocument(
        IFormFile file,
        [FromQuery] string? handle,
        CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaxProductDocumentBytes)
        {
            return BadRequest($"{file.FileName} must be between 1 byte and 20 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!ProductDocumentExtensions.Contains(extension))
        {
            return BadRequest($"{file.FileName} must be a PDF, JPG, PNG, or WEBP file.");
        }

        var safeHandle = BuildSafeMediaHandle(handle);
        var safeFileName = BuildSafeMediaFileName(file.FileName);
        var storagePath = $"commerce/products/{safeHandle}/documents/{Guid.NewGuid():N}_{safeFileName}";
        var contentType = GetProductDocumentContentType(file, extension);

        await using var stream = file.OpenReadStream();
        var upload = await uploadClient.UploadFileAsync(safeFileName, stream, contentType, storagePath, true, cancellationToken);
        if (upload is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, $"Upload failed for {file.FileName}.");
        }

        upload.FileReference = BuildProductMediaReference(upload.UploadId);
        return Ok(upload);
    }

    /// <summary>
    /// Redirects a stored Commerce product media reference to a fresh signed storage URL.
    /// </summary>
    [HttpGet("products/media/{uploadId}")]
    [RequirePermission(MalievPermissions.Commerce.ProductsRead, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> GetProductMedia(string uploadId, CancellationToken cancellationToken)
    {
        var signedUrl = await uploadClient.GetDownloadUrlAsync(uploadId, cancellationToken);
        return string.IsNullOrWhiteSpace(signedUrl) ? NotFound() : Redirect(signedUrl);
    }

    /// <summary>
    /// Uploads collection media images to central storage and returns stable media references.
    /// </summary>
    [HttpPost("collections/media")]
    [RequirePermission(MalievPermissions.Commerce.CollectionsUpdate, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<List<BffUploadResponse>>> UploadCollectionMedia(
        [FromForm] List<IFormFile> files,
        [FromQuery] string? handle,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return BadRequest("No image files uploaded.");
        }

        var safeHandle = BuildSafeMediaHandle(handle);
        var results = new List<BffUploadResponse>(files.Count);

        foreach (var file in files)
        {
            if (file.Length is <= 0 or > MaxProductMediaBytes)
            {
                return BadRequest($"{file.FileName} must be between 1 byte and 10 MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!ProductMediaExtensions.Contains(extension))
            {
                return BadRequest($"{file.FileName} must be a JPG, PNG, or WEBP image.");
            }

            var safeFileName = BuildSafeMediaFileName(file.FileName);
            var storagePath = $"commerce/collections/{safeHandle}/media/{Guid.NewGuid():N}_{safeFileName}";
            var contentType = GetProductMediaContentType(file, extension);

            await using var stream = file.OpenReadStream();
            var upload = await uploadClient.UploadFileAsync(safeFileName, stream, contentType, storagePath, true, cancellationToken);
            if (upload is null)
            {
                return StatusCode(StatusCodes.Status502BadGateway, $"Upload failed for {file.FileName}.");
            }

            upload.FileReference = BuildCollectionMediaReference(upload.UploadId);
            results.Add(upload);
        }

        return Ok(results);
    }

    /// <summary>
    /// Redirects a stored Commerce collection media reference to a fresh signed storage URL.
    /// </summary>
    [HttpGet("collections/media/{uploadId}")]
    [RequirePermission(MalievPermissions.Commerce.CollectionsRead, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> GetCollectionMedia(string uploadId, CancellationToken cancellationToken)
    {
        var signedUrl = await uploadClient.GetDownloadUrlAsync(uploadId, cancellationToken);
        return string.IsNullOrWhiteSpace(signedUrl) ? NotFound() : Redirect(signedUrl);
    }

    /// <summary>
    /// Archives a product listing.
    /// </summary>
    [HttpDelete("products/{id:guid}")]
    [RequirePermission(MalievPermissions.Commerce.ProductsDelete, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> ArchiveProduct(Guid id, CancellationToken cancellationToken)
    {
        var archived = await client.ArchiveProductAsync(id, cancellationToken);
        return archived ? NoContent() : NotFound();
    }

    /// <summary>
    /// Lists product collections.
    /// </summary>
    [HttpGet("collections")]
    [RequirePermission(MalievPermissions.Commerce.CollectionsRead, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<IReadOnlyList<CommerceCollectionDto>>> ListCollections(CancellationToken cancellationToken)
    {
        var result = await client.ListManagedCollectionsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a product collection.
    /// </summary>
    [HttpPost("collections")]
    [RequirePermission(MalievPermissions.Commerce.CollectionsCreate, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<CommerceCollectionDto>> CreateCollection([FromBody] CommerceCollectionMutationRequest request, CancellationToken cancellationToken)
    {
        var result = await client.CreateCollectionAsync(request, cancellationToken);
        return result is null ? BadRequest() : Created(string.Empty, result);
    }

    /// <summary>
    /// Updates a product collection.
    /// </summary>
    [HttpPut("collections/{id:guid}")]
    [RequirePermission(MalievPermissions.Commerce.CollectionsUpdate, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<CommerceCollectionDto>> UpdateCollection(Guid id, [FromBody] CommerceCollectionMutationRequest request, CancellationToken cancellationToken)
    {
        var result = await client.UpdateCollectionAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Unpublishes a product collection.
    /// </summary>
    [HttpDelete("collections/{id:guid}")]
    [RequirePermission(MalievPermissions.Commerce.CollectionsDelete, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> UnpublishCollection(Guid id, CancellationToken cancellationToken)
    {
        var unpublished = await client.UnpublishCollectionAsync(id, cancellationToken);
        return unpublished ? NoContent() : NotFound();
    }

    private static CommerceBomPdfData BuildCommerceBomPdfData(CommerceProductDto product)
    {
        var orderedItems = product.BomItems
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ItemName)
            .ToList();
        var currency = orderedItems.FirstOrDefault()?.Currency ?? "THB";
        var items = orderedItems.Select((item, index) => new CommerceBomPdfItem
        {
            Index = index + 1,
            ItemName = item.ItemName,
            PartNumber = item.PartNumber,
            AssemblyName = item.AssemblyName,
            SubassemblyName = item.SubassemblyName,
            ImageUrl = item.ImageUrl,
            DrawingUrl = item.DrawingUrl,
            SupplierName = item.SupplierName,
            SupplierUrl = item.SupplierUrl,
            Specification = item.Specification,
            Quantity = item.Quantity,
            Unit = item.Unit,
            UnitCost = item.UnitCost,
            Currency = item.Currency,
            LineTotal = item.LineTotal == 0m ? decimal.Round(item.Quantity * item.UnitCost, 2, MidpointRounding.AwayFromZero) : item.LineTotal,
            LeadTimeDays = item.LeadTimeDays,
            SourcingTimeDays = CalculateBomItemSourcingDays(item),
            Notes = item.Notes
        }).ToList();

        return new CommerceBomPdfData
        {
            ProductTitle = product.Title,
            ProductHandle = product.Handle,
            Brand = product.Brand,
            ProductType = product.ProductType,
            Status = product.Status,
            GeneratedAt = DateTime.UtcNow,
            Currency = currency,
            Items = items,
            TotalCost = items.Where(item => string.Equals(item.Currency, currency, StringComparison.OrdinalIgnoreCase)).Sum(item => item.LineTotal),
            SourcingTimeDays = items.Select(item => item.SourcingTimeDays.GetValueOrDefault()).DefaultIfEmpty(0).Max()
        };
    }

    private static int CalculateBomItemSourcingDays(CommerceProductBomItemDto item)
    {
        return Math.Max(0, item.LeadTimeDays.GetValueOrDefault()) + Math.Max(0, item.SourcingTimeDays.GetValueOrDefault());
    }

    private static string BuildProductMediaReference(string uploadId)
    {
        return $"api/v1/commerce/products/media/{Uri.EscapeDataString(uploadId)}";
    }

    private static string BuildCollectionMediaReference(string uploadId)
    {
        return $"api/v1/commerce/collections/media/{Uri.EscapeDataString(uploadId)}";
    }

    private static string BuildSafeMediaHandle(string? handle)
    {
        var source = string.IsNullOrWhiteSpace(handle) ? "draft" : handle;
        var safe = new string(source
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) || character == '-' ? character : '-')
            .ToArray());

        return string.IsNullOrWhiteSpace(safe) ? "draft" : safe;
    }

    private static string BuildSafeMediaFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(name) ? "product-media" : name;
    }

    private static string GetProductMediaContentType(IFormFile file, string extension)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return file.ContentType;
        }

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string GetProductDocumentContentType(IFormFile file, string extension)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
             file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)))
        {
            return file.ContentType;
        }

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string BuildBomDownloadFileName(string handle)
    {
        var safeHandle = new string(handle.Select(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());

        return $"commerce-bom-{safeHandle}.pdf";
    }
}
