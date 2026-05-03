using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Represents a purchase order issued to a supplier for materials or services.
/// </summary>
public sealed record PurchaseOrderDto
{
    /// <summary>The unique integer identifier assigned by PurchaseOrderService.</summary>
    public int Id { get; set; }

    /// <summary>The human-readable purchase order number.</summary>
    public string PoNumber { get; set; } = string.Empty;

    /// <summary>The order type reported by PurchaseOrderService.</summary>
    public string OrderType { get; set; } = "Internal";

    /// <summary>The current lifecycle status of the order.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The supplier identifier assigned by the purchasing service.</summary>
    public int SupplierId { get; set; }

    /// <summary>The supplier identifier assigned by SupplierService.</summary>
    public Guid? SupplierServiceId { get; set; }

    /// <summary>The display name of the supplier receiving the order.</summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>The source customer or sales order identifier associated with this purchase order.</summary>
    public int OrderId { get; set; }

    /// <summary>The source order identifier assigned by OrderService.</summary>
    public string? SourceOrderId { get; set; }

    /// <summary>The optional customer purchase order reference.</summary>
    public string? CustomerPo { get; set; }

    /// <summary>The currency identifier assigned by the currency service.</summary>
    public int CurrencyId { get; set; }

    /// <summary>The currency identifier assigned by CurrencyService.</summary>
    public Guid? CurrencyServiceId { get; set; }

    /// <summary>The ISO currency code used by this purchase order.</summary>
    public string CurrencyCode { get; set; } = "THB";

    /// <summary>The currency symbol used for display when available.</summary>
    public string? CurrencySymbol { get; set; }

    /// <summary>The date when the purchase order was issued or created.</summary>
    public DateTime Date { get; set; }

    /// <summary>The date and time when the purchase order record was created.</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>The optional date when the delivery of items is expected.</summary>
    public DateTime? ExpectedDelivery { get; set; }

    /// <summary>The subtotal amount before withholding tax.</summary>
    public decimal SubtotalAmount { get; set; }

    /// <summary>The total monetary amount of the purchase order.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>The withholding tax rate applied to this purchase order.</summary>
    public decimal? WhtRate { get; set; }

    /// <summary>The withholding tax amount applied to this purchase order.</summary>
    public decimal? WhtAmount { get; set; }

    /// <summary>The supplier contact information captured by PurchaseOrderService.</summary>
    public string? SupplierContactInfo { get; set; }

    /// <summary>Operational notes attached to this purchase order.</summary>
    public string? Notes { get; set; }

    /// <summary>The optimistic concurrency token for update operations.</summary>
    public string RowVersion { get; set; } = string.Empty;

    /// <summary>The user who created this purchase order.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>The user who last modified this purchase order.</summary>
    public string? LastModifiedBy { get; set; }

    /// <summary>The date and time when this purchase order was last modified.</summary>
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>The user who approved this purchase order.</summary>
    public string? ApprovedBy { get; set; }

    /// <summary>The date and time when this purchase order was approved.</summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>The total calculated value of the purchase order.</summary>
    public decimal TotalValue => TotalAmount;

    /// <summary>The number of distinct order items included in the purchase order.</summary>
    public int ItemsCount => Items.Count;

    /// <summary>The collection of individual order items within this purchase order.</summary>
    public List<PurchaseOrderLineItemDto> Items { get; set; } = [];

    /// <summary>The files attached to this purchase order.</summary>
    public List<PurchaseOrderFileDto> Files { get; set; } = [];
}

/// <summary>
/// Represents an individual item line within a purchase order.
/// </summary>
public sealed record PurchaseOrderLineItemDto
{
    /// <summary>The unique identifier of the cached purchase order item.</summary>
    public int Id { get; set; }

    /// <summary>The external order item identifier that PurchaseOrderService uses to hydrate product data.</summary>
    public int ExternalOrderItemId { get; set; }

    /// <summary>The source order item identifier assigned by OrderService.</summary>
    public string? SourceOrderItemId { get; set; }

    /// <summary>The product code copied from the source order item.</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>The product name copied from the source order item.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>The quantity requested.</summary>
    public decimal Quantity { get; set; }

    /// <summary>The unit of measure for the requested quantity.</summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>The price per unit for the item.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>The ISO currency code for the line item.</summary>
    public string Currency { get; set; } = "THB";

    /// <summary>The calculated line total.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>Optional notes for the line item.</summary>
    public string? Notes { get; set; }

    /// <summary>The date and time when external item data was cached.</summary>
    public DateTime CachedAt { get; set; }

    /// <summary>Indicates whether the source order item has changed since it was cached.</summary>
    public bool ExternallyModified { get; set; }
}

/// <summary>
/// Represents a file attached to a purchase order.
/// </summary>
public sealed record PurchaseOrderFileDto
{
    /// <summary>The unique file record identifier.</summary>
    public int Id { get; set; }

    /// <summary>The purchase order identifier that owns this file.</summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>The original file name.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>The storage object name.</summary>
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>The file size in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>The content type of the file.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>The document classification reported by PurchaseOrderService.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>The date and time when the file was uploaded.</summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>The user who uploaded the file.</summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>Optional file description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Data required to create a new purchase order for a supplier.
/// </summary>
public sealed record CreatePurchaseOrderRequest
{
    /// <summary>The order type value expected by PurchaseOrderService. Internal = 0, External = 1.</summary>
    [Required]
    public int OrderType { get; set; }

    /// <summary>The supplier identifier assigned by the purchasing service.</summary>
    public int SupplierId { get; set; }

    /// <summary>The supplier identifier assigned by SupplierService.</summary>
    public Guid? SupplierServiceId { get; set; }

    /// <summary>The source sales or customer order identifier.</summary>
    public int OrderId { get; set; }

    /// <summary>The source sales or customer order identifier assigned by OrderService.</summary>
    public string? SourceOrderId { get; set; }

    /// <summary>Optional customer purchase order reference.</summary>
    public string? CustomerPo { get; set; }

    /// <summary>The currency identifier assigned by CurrencyService.</summary>
    public int CurrencyId { get; set; } = 1;

    /// <summary>The currency identifier assigned by CurrencyService.</summary>
    public Guid? CurrencyServiceId { get; set; }

    /// <summary>The ISO 4217 currency code selected for the purchase order.</summary>
    public string? CurrencyCode { get; set; }

    /// <summary>The withholding tax rate to apply.</summary>
    [Range(0, 100)]
    public decimal WhtRate { get; set; }

    /// <summary>The expected delivery date.</summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>Optional purchase order notes.</summary>
    public string? Notes { get; set; }

    /// <summary>The source order items and quantities included in this purchase order.</summary>
    [Required]
    public List<PurchaseOrderLineItemDto> Items { get; set; } = [];
}

/// <summary>
/// Request body used to cancel a purchase order.
/// </summary>
public sealed record CancelPurchaseOrderRequest
{
    /// <summary>The reason for cancelling the purchase order.</summary>
    [Required]
    public string Reason { get; set; } = string.Empty;
}
