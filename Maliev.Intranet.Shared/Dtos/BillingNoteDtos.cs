using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for a billing note.
/// </summary>
public sealed record BillingNoteDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the billing note number.</summary>
    public string? BillingNoteNumber { get; set; }
    /// <summary>Gets or sets the customer ID.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the collection of associated invoice IDs.</summary>
    public List<Guid> InvoiceIds { get; set; } = [];
    /// <summary>Gets or sets the issue date.</summary>
    public DateTime IssueDate { get; set; }
    /// <summary>Gets or sets the due date.</summary>
    public DateTime DueDate { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request model for creating a billing note.
/// </summary>
public sealed record CreateBillingNoteRequest
{
    /// <summary>Gets or sets the customer ID.</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the collection of invoice IDs to include.</summary>
    [Required]
    public List<Guid> InvoiceIds { get; set; } = [];

    /// <summary>Gets or sets the issue date.</summary>
    [Required]
    public DateTime IssueDate { get; set; }

    /// <summary>Gets or sets the due date.</summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request model for updating an existing billing note.
/// </summary>
public sealed record UpdateBillingNoteRequest
{
    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
    /// <summary>Gets or sets the optional new due date.</summary>
    public DateTime? DueDate { get; set; }
}
