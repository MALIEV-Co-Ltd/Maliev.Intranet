using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Invoice summary DTO.
/// </summary>
public class InvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Balance { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed invoice information.
/// </summary>
public class InvoiceDetailDto
{
    public Guid Id { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? ParentInvoiceId { get; set; }
    public string? ParentInvoiceNumber { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerTaxId { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string? ShippingAddress { get; set; }
    public string? QuotationReference { get; set; }
    public string? PoNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal? ExchangeRate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal WithholdingTaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public int PaymentTermsDays { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public string? FinalizedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
    public string? PdfFileReference { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = [];
    public List<InvoiceSummaryDto> ChildInvoices { get; set; } = [];
    public List<PaymentSummaryDto> Payments { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Payment summary DTO.
/// </summary>
public class PaymentSummaryDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime PaymentDate { get; set; }
}

/// <summary>
/// Detailed payment information.
/// </summary>
public class PaymentDetailDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime? VoidedAt { get; set; }
}

/// <summary>
/// Statistics for payments.
/// </summary>
public class PaymentStatsDto
{
    public decimal TodayTotal { get; set; }
    public double PercentageChangeFromYesterday { get; set; }
    public decimal PendingTotal { get; set; }
    public int PendingCount { get; set; }
    public decimal MonthTotal { get; set; }
    public decimal FailedTotal { get; set; }
    public int FailedCount { get; set; }
}

public sealed record InvoiceItemDto
{
    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public decimal TaxRate { get; set; } = 7.00m;
}

public sealed record CreateInvoiceRequest
{
    [Required]
    public Guid CustomerId { get; set; }

    public BillingIdentityType BillingIdentityType { get; set; }

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public int PaymentTermsDays { get; set; }

    public string Currency { get; set; } = string.Empty;

    public List<InvoiceItemDto> Items { get; set; } = [];
}

public sealed record UpdateInvoiceRequest
{
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceItemDto>? Items { get; set; }
}

public sealed record SplitInvoiceRequest
{
    [Required]
    public List<InvoiceSplitDetail> Splits { get; set; } = [];

    public string? Reason { get; set; }
}

public sealed record InvoiceSplitDetail
{
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string? Notes { get; set; }
}

public sealed record CancelInvoiceRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public sealed record CreatePaymentRequest
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; }
}

public sealed record AllocatePaymentRequest
{
    [Required]
    public List<Guid> InvoiceIds { get; set; } = [];
}

public sealed record VoidPaymentRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}
