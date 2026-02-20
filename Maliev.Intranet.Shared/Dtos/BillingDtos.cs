namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Defines the type of billing document.
/// </summary>
public enum DocumentType
{
    TaxInvoice = 1,
    Invoice = 2,
    CreditNote = 3,
    DebitNote = 4
}

/// <summary>
/// DTO for a credit term.
/// </summary>
public sealed record CreditTermDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Days { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Represents a child invoice from a split operation.
/// </summary>
public sealed record ChildInvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}
