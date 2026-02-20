using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

public sealed record PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime CreatedDate => Date;
    public DateTime? ExpectedDelivery { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TotalValue => TotalAmount;
    public int ItemsCount => Items.Count;
    public List<PurchaseOrderLineItemDto> Items { get; set; } = [];
}

public sealed record PurchaseOrderLineItemDto
{
    public Guid MaterialId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed record CreatePurchaseOrderRequest
{
    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public List<PurchaseOrderLineItemDto> Items { get; set; } = [];
}
