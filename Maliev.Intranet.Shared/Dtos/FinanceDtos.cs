using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Invoice summary DTO.
/// </summary>
public class InvoiceSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the total invoice amount.</summary>
    public decimal Total { get; set; }
    /// <summary>Gets or sets the remaining balance to be paid.</summary>
    public decimal Balance { get; set; }
    /// <summary>Gets or sets the date the invoice was issued.</summary>
    public DateTime IssueDate { get; set; }
    /// <summary>Gets or sets the payment due date.</summary>
    public DateTime DueDate { get; set; }
    /// <summary>Gets or sets the current status of the invoice.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed invoice information.
/// </summary>
public class InvoiceDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the invoice number.</summary>
    public string? InvoiceNumber { get; set; }
    /// <summary>Gets or sets the parent invoice ID, if this is a split invoice.</summary>
    public Guid? ParentInvoiceId { get; set; }
    /// <summary>Gets or sets the parent invoice number.</summary>
    public string? ParentInvoiceNumber { get; set; }
    /// <summary>Gets or sets the customer ID.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer tax ID.</summary>
    public string CustomerTaxId { get; set; } = string.Empty;
    /// <summary>Gets or sets the billing address.</summary>
    public string BillingAddress { get; set; } = string.Empty;
    /// <summary>Gets or sets the shipping address.</summary>
    public string? ShippingAddress { get; set; }
    /// <summary>Gets or sets the associated quotation reference.</summary>
    public string? QuotationReference { get; set; }
    /// <summary>Gets or sets the associated PO number.</summary>
    public string? PoNumber { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = "THB";
    /// <summary>Gets or sets the exchange rate used, if applicable.</summary>
    public decimal? ExchangeRate { get; set; }
    /// <summary>Gets or sets the subtotal amount before tax.</summary>
    public decimal SubTotal { get; set; }
    /// <summary>Gets or sets the tax amount.</summary>
    public decimal TaxAmount { get; set; }
    /// <summary>Gets or sets the withholding tax amount.</summary>
    public decimal WithholdingTaxAmount { get; set; }
    /// <summary>Gets or sets the total invoice amount.</summary>
    public decimal Total { get; set; }
    /// <summary>Gets or sets the issue date.</summary>
    public DateTime IssueDate { get; set; }
    /// <summary>Gets or sets the due date.</summary>
    public DateTime DueDate { get; set; }
    /// <summary>Gets or sets the payment terms in days.</summary>
    public int PaymentTermsDays { get; set; }
    /// <summary>Gets or sets the timestamp when the invoice was finalized.</summary>
    public DateTime? FinalizedAt { get; set; }
    /// <summary>Gets or sets the identifier of the person who finalized the invoice.</summary>
    public string? FinalizedBy { get; set; }
    /// <summary>Gets or sets the timestamp when the invoice was cancelled.</summary>
    public DateTime? CancelledAt { get; set; }
    /// <summary>Gets or sets the identifier of the person who cancelled the invoice.</summary>
    public string? CancelledBy { get; set; }
    /// <summary>Gets or sets the reason for cancellation.</summary>
    public string? CancellationReason { get; set; }
    /// <summary>Gets or sets the cloud storage reference for the PDF file.</summary>
    public string? PdfFileReference { get; set; }
    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
    /// <summary>Gets or sets the list of items in the invoice.</summary>
    public List<InvoiceItemDto> Items { get; set; } = [];
    /// <summary>Gets or sets the list of associated child invoices.</summary>
    public List<InvoiceSummaryDto> ChildInvoices { get; set; } = [];
    /// <summary>Gets or sets the list of payments received for this invoice.</summary>
    public List<PaymentSummaryDto> Payments { get; set; } = [];
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Data transfer object for currency.
/// </summary>
public class CurrencyDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the currency code (ISO 4217).</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the currency name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the currency symbol.</summary>
    public string Symbol { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of decimal places.</summary>
    public int DecimalPlaces { get; set; }
    /// <summary>Gets or sets the current exchange rate to THB.</summary>
    public decimal ExchangeRate { get; set; }
    /// <summary>Gets or sets a value indicating whether the currency is active.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Payment summary DTO.
/// </summary>
public class PaymentSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the unique payment number.</summary>
    public string PaymentNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the associated invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the payment amount.</summary>
    public decimal Amount { get; set; }
    /// <summary>Gets or sets the payment method category.</summary>
    public string Method { get; set; } = string.Empty;
    /// <summary>Gets or sets the detailed payment method name.</summary>
    public string PaymentMethod { get; set; } = string.Empty;
    /// <summary>Gets or sets the current status of the payment.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the date the payment was made.</summary>
    public DateTime PaymentDate { get; set; }
}

/// <summary>
/// Detailed payment information.
/// </summary>
public class PaymentDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the payment number.</summary>
    public string PaymentNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the associated invoice ID.</summary>
    public Guid? InvoiceId { get; set; }
    /// <summary>Gets or sets the associated invoice number.</summary>
    public string? InvoiceNumber { get; set; }
    /// <summary>Gets or sets the customer name.</summary>
    public string? CustomerName { get; set; }
    /// <summary>Gets or sets the payment amount.</summary>
    public decimal Amount { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = "THB";
    /// <summary>Gets or sets the payment method.</summary>
    public string Method { get; set; } = string.Empty;
    /// <summary>Gets or sets the detailed payment method.</summary>
    public string PaymentMethod { get; set; } = string.Empty;
    /// <summary>Gets or sets the payment status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the transaction identifier from the payment gateway.</summary>
    public string TransactionId { get; set; } = string.Empty;
    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
    /// <summary>Gets or sets the identifier of the creator.</summary>
    public string? CreatedBy { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the date the payment was made.</summary>
    public DateTime PaymentDate { get; set; }
    /// <summary>Gets or sets the timestamp when the payment was voided.</summary>
    public DateTime? VoidedAt { get; set; }
}

/// <summary>
/// Statistics for payments.
/// </summary>
public class PaymentStatsDto
{
    /// <summary>Gets or sets the total payment amount for today.</summary>
    public decimal TodayTotal { get; set; }
    /// <summary>Gets or sets the percentage change compared to yesterday.</summary>
    public double PercentageChangeFromYesterday { get; set; }
    /// <summary>Gets or sets the total amount of pending payments.</summary>
    public decimal PendingTotal { get; set; }
    /// <summary>Gets or sets the count of pending payments.</summary>
    public int PendingCount { get; set; }
    /// <summary>Gets or sets the total payment amount for the current month.</summary>
    public decimal MonthTotal { get; set; }
    /// <summary>Gets or sets the total amount of failed payments.</summary>
    public decimal FailedTotal { get; set; }
    /// <summary>Gets or sets the count of failed payments.</summary>
    public int FailedCount { get; set; }
}

/// <summary>
/// Data transfer object for an invoice line item.
/// </summary>
public sealed record InvoiceItemDto
{
    /// <summary>Gets or sets the item description.</summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the quantity.</summary>
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    /// <summary>Gets or sets the unit price.</summary>
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the tax rate percentage.</summary>
    public decimal TaxRate { get; set; } = 7.00m;
}

/// <summary>
/// Request model for creating a new invoice.
/// </summary>
public sealed record CreateInvoiceRequest
{
    /// <summary>Gets or sets the customer ID.</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the billing identity type.</summary>
    public BillingIdentityType BillingIdentityType { get; set; }

    /// <summary>Gets or sets the issue date.</summary>
    [Required]
    public DateTime IssueDate { get; set; }

    /// <summary>Gets or sets the due date.</summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>Gets or sets the payment terms in days.</summary>
    public int PaymentTermsDays { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = "THB";

    /// <summary>Gets or sets the list of items to include in the invoice.</summary>
    public List<InvoiceItemDto> Items { get; set; } = [];
}

/// <summary>
/// Request model for updating an existing invoice.
/// </summary>
public sealed record UpdateInvoiceRequest
{
    /// <summary>Gets or sets the updated status.</summary>
    public string? Status { get; set; }
    /// <summary>Gets or sets the updated due date.</summary>
    public DateTime? DueDate { get; set; }
    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
    /// <summary>Gets or sets the updated list of items.</summary>
    public List<InvoiceItemDto>? Items { get; set; }
}

/// <summary>
/// Request model for splitting an invoice into multiple parts.
/// </summary>
public sealed record SplitInvoiceRequest
{
    /// <summary>Gets or sets the collection of split details.</summary>
    [Required]
    public List<InvoiceSplitDetail> Splits { get; set; } = [];

    /// <summary>Gets or sets the reason for splitting the invoice.</summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Detailed information for a single part of a split invoice.
/// </summary>
public sealed record InvoiceSplitDetail
{
    /// <summary>Gets or sets the percentage of the original total for this split.</summary>
    public decimal Percentage { get; set; }
    /// <summary>Gets or sets the absolute amount for this split.</summary>
    public decimal Amount { get; set; }
    /// <summary>Gets or sets the due date for this part.</summary>
    public DateTime DueDate { get; set; }
    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request model for cancelling an invoice.
/// </summary>
public sealed record CancelInvoiceRequest
{
    /// <summary>Gets or sets the reason for cancellation.</summary>
    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request model for recording a payment.
/// </summary>
public sealed record CreatePaymentRequest
{
    /// <summary>Gets or sets the associated invoice ID.</summary>
    [Required]
    public Guid InvoiceId { get; set; }

    /// <summary>Gets or sets the payment amount.</summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the payment method.</summary>
    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Gets or sets the date the payment was made.</summary>
    public DateTime PaymentDate { get; set; }
}

/// <summary>
/// Request model for allocating a payment across multiple invoices.
/// </summary>
public sealed record AllocatePaymentRequest
{
    /// <summary>Gets or sets the collection of invoice IDs to allocate to.</summary>
    [Required]
    public List<Guid> InvoiceIds { get; set; } = [];
}

/// <summary>
/// Request model for voiding a payment.
/// </summary>
public sealed record VoidPaymentRequest
{
    /// <summary>Gets or sets the reason for voiding the payment.</summary>
    [Required]
    public string Reason { get; set; } = string.Empty;
}
