namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Specifies the type of billing or accounting document.
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// A formal invoice that includes tax information required by regulatory authorities.
    /// </summary>
    TaxInvoice = 1,

    /// <summary>
    /// A standard request for payment for goods or services rendered.
    /// </summary>
    Invoice = 2,

    /// <summary>
    /// A document issued to a buyer to reduce the amount they owe from a previously issued invoice.
    /// </summary>
    CreditNote = 3,

    /// <summary>
    /// A document issued by a seller to a buyer to notify them of an increase in the amount owed.
    /// </summary>
    DebitNote = 4
}

/// <summary>
/// Data transfer object representing a credit term applied to customer payments.
/// </summary>
public sealed record CreditTermDto
{
    /// <summary>
    /// The unique code identifying the credit term.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the credit term (e.g., "Net 30").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A detailed description of the payment conditions.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The number of days allowed for payment before the invoice becomes overdue.
    /// </summary>
    public int Days { get; set; }

    /// <summary>
    /// Indicates whether the credit term is available for use.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Represents a summary of a child invoice generated during a split billing operation.
/// </summary>
public sealed record ChildInvoiceSummaryDto
{
    /// <summary>
    /// Unique identifier for the child invoice.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The assigned invoice number for the child document.
    /// </summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// The total amount for the child invoice, including all taxes and fees.
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// The current lifecycle status of the child invoice.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The date by which payment for the child invoice is expected.
    /// </summary>
    public DateTime DueDate { get; set; }
}
