using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Represents a payment receipt issued to a customer for products or services.
/// </summary>
public sealed record ReceiptDto
{
    /// <summary>The unique identifier of the receipt record.</summary>
    public Guid Id { get; set; }

    /// <summary>The unique human-readable receipt number (e.g., REC-2023-001).</summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>The identifier of the associated invoice for this receipt.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>The display name of the customer receiving the receipt.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>The date when the receipt was issued.</summary>
    public DateTime Date { get; set; }

    /// <summary>The unique identifier of the customer in the system.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>The total monetary amount for this receipt.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>The total amount of the payment received.</summary>
    public decimal Amount => TotalAmount;

    /// <summary>The mode of payment used for this transaction (e.g., "Bank Transfer", "Credit Card").</summary>
    public string PaymentMethod { get; set; } = "Bank Transfer";

    /// <summary>The current status of the receipt record.</summary>
    public string Status { get; set; } = "Valid";

    /// <summary>The list of individual line items that make up the total receipt amount.</summary>
    public List<ReceiptLineItemDto> Lines { get; set; } = [];
}

/// <summary>
/// Represents an individual line item on a payment receipt.
/// </summary>
public sealed record ReceiptLineItemDto
{
    /// <summary>The description of the product or service provided.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The amount assigned to this specific receipt line.</summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Data required to create a new payment receipt for a customer.
/// </summary>
public sealed record CreateReceiptRequest
{
    /// <summary>The identifier of the customer for whom the receipt is created.</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>The issue date for the receipt.</summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>The list of receipt line items and amounts to include.</summary>
    [Required]
    public List<ReceiptLineItemDto> Lines { get; set; } = [];
}
