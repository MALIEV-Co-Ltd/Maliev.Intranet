using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for a purchase order.
/// </summary>
public sealed record PurchaseOrderDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the internal order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the PO number (external).</summary>
    public string? CustomerPO { get; set; }
    /// <summary>Gets or sets the supplier ID.</summary>
    public int SupplierID { get; set; }
    /// <summary>Gets or sets the supplier name.</summary>
    public string SupplierName { get; set; } = string.Empty;
    /// <summary>Gets or sets the order date.</summary>
    public DateTime OrderDate { get; set; }
    /// <summary>Gets or sets the expected delivery date.</summary>
    public DateTime? ExpectedDeliveryDate { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the total order amount.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the collection of line items.</summary>
    public List<PurchaseOrderLineItemDto> Items { get; set; } = [];
}

/// <summary>
/// Data transfer object for a line item within a purchase order.
/// </summary>
public sealed record PurchaseOrderLineItemDto
{
    /// <summary>Gets or sets the material ID.</summary>
    public int MaterialID { get; set; }
    /// <summary>Gets or sets the quantity to order.</summary>
    public decimal Quantity { get; set; }
    /// <summary>Gets or sets the unit price.</summary>
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Request model for creating a purchase order.
/// </summary>
public sealed record CreatePurchaseOrderRequest
{
    /// <summary>Gets or sets the target supplier ID.</summary>
    [Required]
    public int SupplierID { get; set; }

    /// <summary>Gets or sets the order date.</summary>
    [Required]
    public DateTime OrderDate { get; set; }

    /// <summary>Gets or sets the currency ID.</summary>
    public int CurrencyID { get; set; } = 1; // Default to THB

    /// <summary>Gets or sets the collection of items to order.</summary>
    [Required]
    public List<PurchaseOrderLineItemDto> Items { get; set; } = [];
}
