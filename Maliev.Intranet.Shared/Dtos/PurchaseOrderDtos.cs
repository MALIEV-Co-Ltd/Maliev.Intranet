using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Represents a purchase order issued to a supplier for materials or services.
/// </summary>
public sealed record PurchaseOrderDto
{
    /// <summary>The unique identifier of the purchase order.</summary>
    public Guid Id { get; set; }

    /// <summary>The unique human-readable purchase order number (e.g., PO-2023-001).</summary>
    public string PoNumber { get; set; } = string.Empty;

    /// <summary>The unique identifier of the supplier receiving the order.</summary>
    public Guid SupplierId { get; set; }

    /// <summary>The display name of the supplier.</summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>The date when the purchase order was issued.</summary>
    public DateTime Date { get; set; }

    /// <summary>The date and time when the purchase order record was created.</summary>
    public DateTime CreatedDate => Date;

    /// <summary>The optional date when the delivery of items is expected.</summary>
    public DateTime? ExpectedDelivery { get; set; }

    /// <summary>The current fulfillment status of the order (e.g., Draft, Sent, Received).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The total monetary amount of the purchase order, including all line items.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>The total calculated value of the purchase order.</summary>
    public decimal TotalValue => TotalAmount;

    /// <summary>The number of distinct material items included in the order.</summary>
    public int ItemsCount => Items.Count;

    /// <summary>The collection of individual material line items within this purchase order.</summary>
    public List<PurchaseOrderLineItemDto> Items { get; set; } = [];
}

/// <summary>
/// Represents an individual item line within a purchase order.
/// </summary>
public sealed record PurchaseOrderLineItemDto
{
    /// <summary>The unique identifier of the material being ordered.</summary>
    public Guid MaterialId { get; set; }

    /// <summary>The quantity of the material requested.</summary>
    public int Quantity { get; set; }

    /// <summary>The price per unit for the specific material in this order.</summary>
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Data required to create a new purchase order for a supplier.
/// </summary>
public sealed record CreatePurchaseOrderRequest
{
    /// <summary>The identifier of the supplier for this order.</summary>
    [Required]
    public Guid SupplierId { get; set; }

    /// <summary>The requested issue date for the purchase order.</summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>The list of material items and quantities to include in the order.</summary>
    [Required]
    public List<PurchaseOrderLineItemDto> Items { get; set; } = [];
}
