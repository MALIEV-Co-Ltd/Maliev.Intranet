namespace Maliev.Intranet.Shared;

/// <summary>
/// Storefront product summary for employee catalog management.
/// </summary>
public sealed class CommerceProductSummaryDto
{
    /// <summary>Gets or sets the product identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the product handle.</summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>Gets or sets the product title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the brand.</summary>
    public string? Brand { get; set; }

    /// <summary>Gets or sets the product summary.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets the product type.</summary>
    public string ProductType { get; set; } = string.Empty;

    /// <summary>Gets or sets the publishing status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the starting price.</summary>
    public decimal StartingPrice { get; set; }

    /// <summary>Gets or sets the currency.</summary>
    public string Currency { get; set; } = "THB";

    /// <summary>Gets or sets the thumbnail URL.</summary>
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// Storefront product detail for employee catalog management.
/// </summary>
public sealed class CommerceProductDto
{
    /// <summary>Gets or sets the product identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the product handle.</summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>Gets or sets the product title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the brand.</summary>
    public string? Brand { get; set; }

    /// <summary>Gets or sets the short summary.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the product type.</summary>
    public string ProductType { get; set; } = "Product";

    /// <summary>Gets or sets the publishing status.</summary>
    public string Status { get; set; } = "Draft";

    /// <summary>Gets or sets product variants.</summary>
    public List<CommerceProductVariantDto> Variants { get; set; } = [];

    /// <summary>Gets or sets product media.</summary>
    public List<CommerceProductMediaDto> Media { get; set; } = [];

    /// <summary>Gets or sets linked collections.</summary>
    public List<CommerceCollectionSummaryDto> Collections { get; set; } = [];
}

/// <summary>
/// Product variant in the Commerce catalog.
/// </summary>
public sealed class CommerceProductVariantDto
{
    /// <summary>Gets or sets the variant identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the SKU.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the variant title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the price amount.</summary>
    public decimal PriceAmount { get; set; }

    /// <summary>Gets or sets the currency.</summary>
    public string Currency { get; set; } = "THB";

    /// <summary>Gets or sets the inventory quantity.</summary>
    public int InventoryQuantity { get; set; }

    /// <summary>Gets or sets whether the variant is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets option values as JSON.</summary>
    public string? OptionValuesJson { get; set; }
}

/// <summary>
/// Product media in the Commerce catalog.
/// </summary>
public sealed class CommerceProductMediaDto
{
    /// <summary>Gets or sets the media identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the media URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets alternative text.</summary>
    public string? AltText { get; set; }

    /// <summary>Gets or sets sort order.</summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Product collection in the Commerce catalog.
/// </summary>
public sealed class CommerceCollectionDto
{
    /// <summary>Gets or sets the collection identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the collection handle.</summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the collection is published.</summary>
    public bool IsPublished { get; set; } = true;
}

/// <summary>
/// Product collection summary.
/// </summary>
public sealed class CommerceCollectionSummaryDto
{
    /// <summary>Gets or sets the collection identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the collection handle.</summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection title.</summary>
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Product mutation request for Commerce catalog management.
/// </summary>
public class CommerceProductMutationRequest
{
    /// <summary>Gets or sets the product handle.</summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>Gets or sets the product title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the brand.</summary>
    public string? Brand { get; set; }

    /// <summary>Gets or sets the summary.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the product type.</summary>
    public string ProductType { get; set; } = "Product";

    /// <summary>Gets or sets the publishing status.</summary>
    public string Status { get; set; } = "Draft";

    /// <summary>Gets or sets variants.</summary>
    public List<CommerceProductVariantMutationRequest> Variants { get; set; } = [];

    /// <summary>Gets or sets media.</summary>
    public List<CommerceProductMediaMutationRequest> Media { get; set; } = [];

    /// <summary>Gets or sets collection handles.</summary>
    public List<string> CollectionHandles { get; set; } = [];
}

/// <summary>
/// Product variant mutation request.
/// </summary>
public sealed class CommerceProductVariantMutationRequest
{
    /// <summary>Gets or sets the SKU.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the variant title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the price amount.</summary>
    public decimal PriceAmount { get; set; }

    /// <summary>Gets or sets the currency.</summary>
    public string Currency { get; set; } = "THB";

    /// <summary>Gets or sets inventory quantity.</summary>
    public int InventoryQuantity { get; set; }

    /// <summary>Gets or sets option values as JSON.</summary>
    public string? OptionValuesJson { get; set; }
}

/// <summary>
/// Product media mutation request.
/// </summary>
public sealed class CommerceProductMediaMutationRequest
{
    /// <summary>Gets or sets the URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets alt text.</summary>
    public string? AltText { get; set; }

    /// <summary>Gets or sets sort order.</summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Collection mutation request.
/// </summary>
public sealed class CommerceCollectionMutationRequest
{
    /// <summary>Gets or sets the collection handle.</summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the collection is published.</summary>
    public bool IsPublished { get; set; } = true;
}

/// <summary>
/// Product status mutation request.
/// </summary>
public sealed class CommerceProductStatusRequest
{
    /// <summary>Gets or sets the publishing status.</summary>
    public string Status { get; set; } = "Draft";
}

/// <summary>
/// Shopify catalog import request used from the Intranet.
/// </summary>
public sealed class CommerceShopifyImportRequest
{
    /// <summary>Gets or sets whether to report changes without writing.</summary>
    public bool DryRun { get; set; }

    /// <summary>Gets or sets the Shopify page size.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Gets or sets an optional Shopify product query.</summary>
    public string? SearchQuery { get; set; }

    /// <summary>Gets or sets allowed product handles.</summary>
    public List<string> ProductHandles { get; set; } = [];

    /// <summary>Gets or sets allowed product type fragments.</summary>
    public List<string> ProductTypes { get; set; } = [];

    /// <summary>Gets or sets required tag fragments.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Gets or sets allowed collection handles.</summary>
    public List<string> CollectionHandles { get; set; } = [];

    /// <summary>Gets or sets keyword fragments.</summary>
    public List<string> Keywords { get; set; } = [];

    /// <summary>Gets or sets an ensured collection handle.</summary>
    public string? EnsureCollectionHandle { get; set; }

    /// <summary>Gets or sets an ensured collection title.</summary>
    public string? EnsureCollectionTitle { get; set; }
}

/// <summary>
/// Shopify catalog import result.
/// </summary>
public sealed class CommerceShopifyImportResult
{
    /// <summary>Gets or sets products read from Shopify.</summary>
    public int ProductsRead { get; set; }

    /// <summary>Gets or sets created product count.</summary>
    public int ProductsCreated { get; set; }

    /// <summary>Gets or sets updated product count.</summary>
    public int ProductsUpdated { get; set; }

    /// <summary>Gets or sets skipped product count.</summary>
    public int ProductsSkipped { get; set; }

    /// <summary>Gets or sets created collection count.</summary>
    public int CollectionsCreated { get; set; }

    /// <summary>Gets or sets whether the import was a dry run.</summary>
    public bool DryRun { get; set; }

    /// <summary>Gets or sets import warnings.</summary>
    public List<string> Warnings { get; set; } = [];
}
