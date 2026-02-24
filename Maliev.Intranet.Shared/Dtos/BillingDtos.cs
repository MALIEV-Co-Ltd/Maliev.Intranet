namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Defines the type of billing document.
/// </summary>
public enum DocumentType
{
    /// <summary>Official tax invoice document.</summary>
    TaxInvoice = 1,
    /// <summary>Standard invoice document.</summary>
    Invoice = 2,
    /// <summary>Credit note for returns or adjustments.</summary>
    CreditNote = 3,
    /// <summary>Debit note for additional charges.</summary>
    DebitNote = 4
}

/// <summary>
/// DTO for a credit term.
/// </summary>
public sealed record CreditTermDto
{
    /// <summary>Gets or sets the unique code for the credit term.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the name of the credit term.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of days for payment.</summary>
    public int Days { get; set; }
    /// <summary>Gets or sets a value indicating whether the credit term is active.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Represents a child invoice from a split operation.
/// </summary>
public sealed record ChildInvoiceSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the invoice number.</summary>
    public string? InvoiceNumber { get; set; }
    /// <summary>Gets or sets the grand total amount.</summary>
    public decimal GrandTotal { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the payment due date.</summary>
    public DateTime DueDate { get; set; }
}
