using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object representing a billing note issued to a customer.
/// </summary>
public sealed record BillingNoteDto
{
    /// <summary>
    /// Unique identifier for the billing note.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique reference number assigned to the billing note.
    /// </summary>
    public string? BillingNoteNumber { get; set; }

    /// <summary>
    /// The unique identifier of the customer receiving the billing note.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// The collection of invoice identifiers associated with this billing note.
    /// </summary>
    public List<Guid> InvoiceIds { get; set; } = [];

    /// <summary>
    /// The date the billing note was issued.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// The date by which payment for the associated invoices is requested.
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// The current lifecycle status of the billing note (e.g., Pending, Completed).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The aggregated total amount of all invoices included in this billing note.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Optional additional remarks or internal comments regarding the billing note.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload for creating a new billing note for a customer.
/// </summary>
public sealed record CreateBillingNoteRequest
{
    /// <summary>
    /// The unique identifier of the customer for whom the billing note is being created.
    /// </summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// The list of invoice identifiers to be included in the new billing note.
    /// </summary>
    [Required]
    public List<Guid> InvoiceIds { get; set; } = [];

    /// <summary>
    /// The intended issue date for the billing note.
    /// </summary>
    [Required]
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// The intended due date for payment requests in this billing note.
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Optional remarks or notes to include with the billing note.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload for updating an existing billing note's metadata.
/// </summary>
public sealed record UpdateBillingNoteRequest
{
    /// <summary>
    /// The updated remarks or notes for the billing note.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// The updated due date for the billing note, if applicable.
    /// </summary>
    public DateTime? DueDate { get; set; }
}
