using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

public sealed record ReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Amount => TotalAmount;
    public string PaymentMethod { get; set; } = "Bank Transfer";
    public string Status { get; set; } = "Valid";
    public List<ReceiptLineItemDto> Lines { get; set; } = [];
}

public sealed record ReceiptLineItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed record CreateReceiptRequest
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public List<ReceiptLineItemDto> Lines { get; set; } = [];
}
