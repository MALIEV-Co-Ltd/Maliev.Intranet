using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for a payment receipt.
/// </summary>
public sealed record ReceiptDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the unique receipt number.</summary>
    public string ReceiptNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the associated invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the receipt date.</summary>
    public DateTime Date { get; set; }
    /// <summary>Gets or sets the customer ID.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the total amount received.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets the total amount (alias for TotalAmount).</summary>
    public decimal Amount => TotalAmount;
    /// <summary>Gets or sets the payment method used.</summary>
    public string PaymentMethod { get; set; } = "Bank Transfer";
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = "Valid";
    /// <summary>Gets or sets the collection of line items in the receipt.</summary>
    public List<ReceiptLineItemDto> Lines { get; set; } = [];
}

/// <summary>
/// Data transfer object for a line item within a receipt.
/// </summary>
public sealed record ReceiptLineItemDto
{
    /// <summary>Gets or sets the line description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the amount for this line.</summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Request model for creating a receipt.
/// </summary>
public sealed record CreateReceiptRequest
{
    /// <summary>Gets or sets the customer ID.</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the receipt date.</summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>Gets or sets the collection of line items to include.</summary>
    [Required]
    public List<ReceiptLineItemDto> Lines { get; set; } = [];
}
