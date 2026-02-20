using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

public sealed record BillingNoteDto
{
    public Guid Id { get; set; }
    public string? BillingNoteNumber { get; set; }
    public Guid CustomerId { get; set; }
    public List<Guid> InvoiceIds { get; set; } = [];
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public sealed record CreateBillingNoteRequest
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public List<Guid> InvoiceIds { get; set; } = [];

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public string? Notes { get; set; }
}

public sealed record UpdateBillingNoteRequest
{
    public string? Notes { get; set; }
    public DateTime? DueDate { get; set; }
}
