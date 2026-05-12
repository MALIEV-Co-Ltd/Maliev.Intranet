using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Employee Commerce catalog management endpoints.
/// </summary>
/// <param name="client">Commerce service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/commerce")]
public sealed class CommerceController(CommerceServiceClient client) : ControllerBase
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

    /// <summary>
    /// Imports the injection molding machine listing from Shopify.
    /// </summary>
    [HttpPost("imports/shopify/injection-molding-machine")]
    [RequirePermission(MalievPermissions.Commerce.ImportsCreate, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<CommerceShopifyImportResult>> ImportInjectionMoldingMachine(
        [FromBody] CommerceShopifyImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await client.ImportInjectionMoldingMachineAsync(request, cancellationToken);
        return result is null ? BadRequest() : Ok(result);
    }
}
