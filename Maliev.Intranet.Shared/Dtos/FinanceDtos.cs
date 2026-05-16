using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Invoice summary DTO for listing and high-level overview purposes.
/// </summary>
public class InvoiceSummaryDto
{
    /// <summary>Gets or sets the unique identifier of the invoice.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the customer the invoice is issued to.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the total invoice amount including taxes.</summary>
    public decimal Total { get; set; }

    /// <summary>Gets or sets the remaining balance to be paid on the invoice.</summary>
    public decimal Balance { get; set; }

    /// <summary>Gets or sets the amount already allocated to the invoice.</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>Gets or sets the date the invoice was issued.</summary>
    public DateTime IssueDate { get; set; }

    /// <summary>Gets or sets the date by which the payment is due.</summary>
    public DateTime DueDate { get; set; }

    /// <summary>Gets or sets the current status of the invoice (e.g., "Draft", "Open", "Paid").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the invoice was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed invoice information including line items, payment history, and related invoices.
/// </summary>
public class InvoiceDetailDto
{
    /// <summary>Gets or sets the unique identifier of the invoice.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable invoice number.</summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>Gets or sets the unique identifier of the parent invoice if this is a split invoice.</summary>
    public Guid? ParentInvoiceId { get; set; }

    /// <summary>Gets or sets the invoice number of the parent invoice.</summary>
    public string? ParentInvoiceNumber { get; set; }

    /// <summary>Gets or sets the unique identifier of the customer.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the name of the customer the invoice is issued to.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax identification number of the customer.</summary>
    public string CustomerTaxId { get; set; } = string.Empty;

    /// <summary>Gets or sets the billing address for the invoice.</summary>
    public string BillingAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the shipping address associated with the order.</summary>
    public string? ShippingAddress { get; set; }

    /// <summary>Gets or sets the reference to the original quotation.</summary>
    public string? QuotationReference { get; set; }

    /// <summary>Gets or sets the customer purchase order number.</summary>
    public string? PoNumber { get; set; }

    /// <summary>Gets or sets the current status of the invoice.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency code used for the invoice (e.g., "THB", "USD").</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Gets or sets the exchange rate applied if the invoice is in a foreign currency.</summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>Gets or sets the subtotal amount before taxes.</summary>
    public decimal SubTotal { get; set; }

    /// <summary>Gets or sets the total tax amount applied to the invoice.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the withholding tax amount deducted if applicable.</summary>
    public decimal WithholdingTaxAmount { get; set; }

    /// <summary>Gets or sets the total invoice amount including taxes.</summary>
    public decimal Total { get; set; }

    /// <summary>Gets or sets the amount already allocated to the invoice.</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>Gets or sets the remaining balance still due on the invoice.</summary>
    public decimal Balance { get; set; }

    /// <summary>Gets or sets the date the invoice was issued.</summary>
    public DateTime IssueDate { get; set; }

    /// <summary>Gets or sets the date by which the payment is due.</summary>
    public DateTime DueDate { get; set; }

    /// <summary>Gets or sets the number of days allowed for payment after issuance.</summary>
    public int PaymentTermsDays { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the invoice was finalized.</summary>
    public DateTime? FinalizedAt { get; set; }

    /// <summary>Gets or sets the name of the user who finalized the invoice.</summary>
    public string? FinalizedBy { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the invoice was cancelled.</summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>Gets or sets the name of the user who cancelled the invoice.</summary>
    public string? CancelledBy { get; set; }

    /// <summary>Gets or sets the reason provided for the cancellation.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>Gets or sets the storage reference to the generated PDF document.</summary>
    public string? PdfFileReference { get; set; }

    /// <summary>Gets or sets additional notes for the invoice.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the collection of individual line items on the invoice.</summary>
    public List<InvoiceItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the collection of invoices that resulted from splitting this invoice.</summary>
    public List<InvoiceSummaryDto> ChildInvoices { get; set; } = [];

    /// <summary>Gets or sets the collection of payment summaries associated with this invoice.</summary>
    public List<PaymentSummaryDto> Payments { get; set; } = [];

    /// <summary>Gets or sets the UTC timestamp when the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Payment summary DTO for listing and tracking transaction status.
/// </summary>
public class PaymentSummaryDto
{
    /// <summary>Gets or sets the unique identifier of the payment.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable payment reference number.</summary>
    public string PaymentNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the invoice number associated with this payment.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the customer who made the payment.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount paid in this transaction.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the payment method description.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the standardized payment method code.</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Gets or sets the current status of the payment (e.g., "Pending", "Completed", "Failed").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the date the payment was actually made.</summary>
    public DateTime PaymentDate { get; set; }
}

/// <summary>
/// Detailed payment information including transaction identifiers and audit details.
/// </summary>
public class PaymentDetailDto
{
    /// <summary>Gets or sets the unique identifier of the payment.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable payment reference number.</summary>
    public string PaymentNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier of the associated invoice.</summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>Gets or sets the human-readable invoice number.</summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>Gets or sets the name of the customer who made the payment.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Gets or sets the amount paid in this transaction.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency code used for the payment.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Gets or sets the payment method description.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the standardized payment method code.</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Gets or sets the current status of the payment.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the external transaction identifier from the payment gateway or bank.</summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Gets or sets additional notes regarding the payment.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the name of the user who recorded the payment.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the date the payment was actually made.</summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>Gets or sets the UTC timestamp if and when the payment was voided.</summary>
    public DateTime? VoidedAt { get; set; }
}

/// <summary>
/// Represents aggregated statistics for payment operations within a given period.
/// </summary>
public class PaymentStatsDto
{
    /// <summary>Gets or sets the total value of payments received today.</summary>
    public decimal TodayTotal { get; set; }

    /// <summary>Gets or sets the percentage change in payment volume compared to yesterday.</summary>
    public double PercentageChangeFromYesterday { get; set; }

    /// <summary>Gets or sets the total value of payments currently in pending status.</summary>
    public decimal PendingTotal { get; set; }

    /// <summary>Gets or sets the number of payments currently in pending status.</summary>
    public int PendingCount { get; set; }

    /// <summary>Gets or sets the total value of payments received in the current month.</summary>
    public decimal MonthTotal { get; set; }

    /// <summary>Gets or sets the total value of failed payment attempts.</summary>
    public decimal FailedTotal { get; set; }

    /// <summary>Gets or sets the number of failed payment attempts.</summary>
    public int FailedCount { get; set; }
}

/// <summary>
/// Represents a single line item within an invoice.
/// </summary>
public sealed record InvoiceItemDto
{
    /// <summary>Gets or sets the description of the product or service provided.</summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the quantity of items or hours being billed.</summary>
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    /// <summary>Gets or sets the price per unit of the item.</summary>
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the tax rate percentage to be applied to this item.</summary>
    public decimal TaxRate { get; set; } = 7.00m;
}

/// <summary>
/// Request payload for creating a new invoice.
/// </summary>
public sealed record CreateInvoiceRequest
{
    /// <summary>Gets or sets the unique identifier of the customer the invoice is for.</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the type of billing identity to use for the invoice.</summary>
    public BillingIdentityType BillingIdentityType { get; set; }

    /// <summary>Gets or sets the customer display name captured on the invoice.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the customer tax ID captured on the invoice.</summary>
    public string CustomerTaxId { get; set; } = string.Empty;

    /// <summary>Gets or sets the billing address captured on the invoice.</summary>
    public string BillingAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional shipping address captured on the invoice.</summary>
    public string? ShippingAddress { get; set; }

    /// <summary>Gets or sets the customer's purchase order number.</summary>
    public string? PoNumber { get; set; }

    /// <summary>Gets or sets the date the invoice is issued.</summary>
    [Required]
    public DateTime IssueDate { get; set; }

    /// <summary>Gets or sets the date by which the invoice must be paid.</summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>Gets or sets the number of days for the payment terms.</summary>
    public int PaymentTermsDays { get; set; }

    /// <summary>Gets or sets the currency code for the invoice.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of items to include in the invoice.</summary>
    public List<InvoiceItemDto> Items { get; set; } = [];
}

/// <summary>
/// Request payload for registering an invoice file already uploaded to storage.
/// </summary>
public sealed record RegisterInvoiceFileRequest
{
    /// <summary>Gets or sets the file type, such as CustomerPO, PDF, or XML.</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>Gets or sets the storage URL or storage path.</summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets or sets the service or user that generated or uploaded the file.</summary>
    public string GeneratedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional checksum.</summary>
    public string? Checksum { get; set; }
}

/// <summary>
/// Response payload for a file linked to an invoice.
/// </summary>
public sealed record InvoiceFileReferenceDto
{
    /// <summary>Gets or sets the unique file reference identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the invoice identifier.</summary>
    public Guid InvoiceId { get; set; }

    /// <summary>Gets or sets the file type.</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>Gets or sets the storage URL or path.</summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets or sets the service or user that generated or uploaded the file.</summary>
    public string GeneratedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional checksum.</summary>
    public string? Checksum { get; set; }

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request payload for updating an existing invoice.
/// </summary>
public sealed record UpdateInvoiceRequest
{
    /// <summary>Gets or sets the updated status of the invoice.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the updated due date for the invoice.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Gets or sets updated notes for the invoice.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the updated collection of line items.</summary>
    public List<InvoiceItemDto>? Items { get; set; }
}

/// <summary>
/// Request payload for splitting an existing invoice into multiple payments or installments.
/// </summary>
public sealed record SplitInvoiceRequest
{
    /// <summary>Gets or sets the collection of split details defining how the invoice is divided.</summary>
    [Required]
    public List<InvoiceSplitDetail> Splits { get; set; } = [];

    /// <summary>Gets or sets the reason for splitting the invoice.</summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Defines the specific parameters for one part of a split invoice.
/// </summary>
public sealed record InvoiceSplitDetail
{
    /// <summary>Gets or sets the percentage of the total amount for this split.</summary>
    public decimal Percentage { get; set; }

    /// <summary>Gets or sets the specific amount for this split.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the due date for this specific split payment.</summary>
    public DateTime DueDate { get; set; }

    /// <summary>Gets or sets additional notes for this split.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload for cancelling an existing invoice.
/// </summary>
public sealed record CancelInvoiceRequest
{
    /// <summary>Gets or sets the reason for the cancellation.</summary>
    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for recording a new payment against an invoice.
/// </summary>
public sealed record CreatePaymentRequest
{
    /// <summary>Gets or sets the unique identifier of the invoice to pay.</summary>
    [Required]
    public Guid InvoiceId { get; set; }

    /// <summary>Gets or sets the amount being paid.</summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the standardized payment method code.</summary>
    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Gets or sets the date the payment was made.</summary>
    public DateTime PaymentDate { get; set; }
}

/// <summary>
/// Request payload for allocating an unapplied payment to one or more invoices.
/// </summary>
public sealed record AllocatePaymentRequest
{
    /// <summary>Gets or sets the collection of invoice identifiers to allocate the payment to.</summary>
    [Required]
    public List<Guid> InvoiceIds { get; set; } = [];
}

/// <summary>
/// Request payload for voiding an existing payment.
/// </summary>
public sealed record VoidPaymentRequest
{
    /// <summary>Gets or sets the reason for voiding the payment.</summary>
    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for recording and allocating an employee-entered invoice payment.
/// </summary>
public sealed record RecordInvoicePaymentRequest
{
    /// <summary>Gets or sets the amount being allocated to the invoice.</summary>
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the payment method, such as Bank Transfer, Cash, Credit Card, or QR Payment.</summary>
    [Required]
    public string PaymentMethod { get; set; } = "Bank Transfer";

    /// <summary>Gets or sets the date the payment was received.</summary>
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    /// <summary>Gets or sets an optional bank, transfer, or gateway reference number.</summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>Gets or sets optional staff notes about the payment evidence.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Response payload for a recorded invoice payment and the updated invoice state.
/// </summary>
public sealed record RecordInvoicePaymentResponse
{
    /// <summary>Gets or sets the created payment identifier.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>Gets or sets the invoice identifier the payment was allocated to.</summary>
    public Guid InvoiceId { get; set; }

    /// <summary>Gets or sets the invoice number after allocation.</summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>Gets or sets the updated invoice status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount allocated by this operation.</summary>
    public decimal AllocatedAmount { get; set; }

    /// <summary>Gets or sets the total amount already allocated to the invoice.</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>Gets or sets the remaining invoice balance after allocation.</summary>
    public decimal Balance { get; set; }

    /// <summary>Gets or sets the updated invoice detail returned by InvoiceService.</summary>
    public InvoiceDetailDto Invoice { get; set; } = new();
}
