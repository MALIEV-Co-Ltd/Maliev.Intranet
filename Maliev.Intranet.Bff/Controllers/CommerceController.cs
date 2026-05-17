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
/// <param name="pdfClient">PDF service client.</param>
/// <param name="httpClientFactory">HTTP client factory for generated PDF downloads.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/commerce")]
public sealed class CommerceController(
    CommerceServiceClient client,
    PdfServiceClient? pdfClient = null,
    IHttpClientFactory? httpClientFactory = null) : ControllerBase
{
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
            Specification = item.Specification,
            Quantity = item.Quantity,
            Unit = item.Unit,
            UnitCost = item.UnitCost,
            Currency = item.Currency,
            LineTotal = item.LineTotal == 0m ? decimal.Round(item.Quantity * item.UnitCost, 2, MidpointRounding.AwayFromZero) : item.LineTotal,
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
            TotalCost = items.Where(item => string.Equals(item.Currency, currency, StringComparison.OrdinalIgnoreCase)).Sum(item => item.LineTotal)
        };
    }

    private static string BuildBomDownloadFileName(string handle)
    {
        var safeHandle = new string(handle.Select(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());

        return $"commerce-bom-{safeHandle}.pdf";
    }
}
