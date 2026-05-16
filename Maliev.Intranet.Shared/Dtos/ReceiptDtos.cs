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

    /// <summary>The unique identifier of the associated invoice for this receipt.</summary>
    public Guid InvoiceId { get; set; }

    /// <summary>The display name of the customer receiving the receipt.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>The date when the receipt was issued.</summary>
    public DateTime Date { get; set; }

    /// <summary>The date when the receipt was issued by ReceiptService.</summary>
    public DateTime IssueDate { get; set; }

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

    /// <summary>The optional generated PDF artifact reference.</summary>
    public Guid? PdfReferenceId { get; set; }

    /// <summary>The UTC timestamp when the receipt was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>The user or service principal that created the receipt.</summary>
    public string CreatedBy { get; set; } = string.Empty;

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
    /// <summary>The unique identifier of the invoice that the receipt is generated from.</summary>
    [Required]
    public Guid InvoiceId { get; set; }

    /// <summary>The receipt amount, which may be a full or partial invoice amount.</summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>The payment method displayed on the receipt.</summary>
    public string? PaymentMethod { get; set; } = "Bank Transfer";

    /// <summary>The optional split-invoice segment identifier for partial segment receipts.</summary>
    public Guid? InvoiceSegmentId { get; set; }
}
