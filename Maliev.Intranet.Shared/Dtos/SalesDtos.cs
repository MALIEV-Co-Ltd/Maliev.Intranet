using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Order summary information.
/// </summary>
public class OrderSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the total quantity.</summary>
    public decimal Total { get; set; }
    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Quotation summary DTO.
/// </summary>
public class QuotationSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the quotation number.</summary>
    public string QuotationNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the total amount.</summary>
    public decimal Total { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed quotation information.
/// </summary>
public class QuotationDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the quotation number.</summary>
    public string QuotationNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer ID.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the associated RFQ ID.</summary>
    public Guid? SourceRfqId { get; set; }
    /// <summary>Gets or sets the associated RFQ number.</summary>
    public string? SourceRfqNumber { get; set; }
    /// <summary>Gets or sets the current version number.</summary>
    public int CurrentVersionNumber { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the start of the validity period.</summary>
    public DateTime ValidityPeriodStart { get; set; }
    /// <summary>Gets or sets the end of the validity period.</summary>
    public DateTime ValidityPeriodEnd { get; set; }
    /// <summary>Gets or sets the subtotal amount.</summary>
    public decimal SubTotal { get; set; }
    /// <summary>Gets or sets the tax amount.</summary>
    public decimal Tax { get; set; }
    /// <summary>Gets or sets the total amount.</summary>
    public decimal Total { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = "THB";
    /// <summary>Gets or sets delivery expectations or notes.</summary>
    public string? DeliveryExpectations { get; set; }
    /// <summary>Gets or sets the collection of quotation versions.</summary>
    public List<QuotationVersionDto> Versions { get; set; } = [];
    /// <summary>Gets or sets internal notes.</summary>
    public List<InternalNoteDto> InternalNotes { get; set; } = [];
    /// <summary>Gets or sets file attachments.</summary>
    public List<FileAttachmentDto> Attachments { get; set; } = [];
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Data transfer object for a quotation version.
/// </summary>
public class QuotationVersionDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the version number.</summary>
    public int VersionNumber { get; set; }
    /// <summary>Gets or sets the collection of line items.</summary>
    public List<QuotationItemDto> LineItems { get; set; } = [];
    /// <summary>Gets or sets the total price for this version.</summary>
    public decimal TotalPrice { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = "THB";
    /// <summary>Gets or sets delivery expectations.</summary>
    public string? DeliveryExpectations { get; set; }
    /// <summary>Gets or sets a summary of changes in this version.</summary>
    public string? ChangeSummary { get; set; }
    /// <summary>Gets or sets the identifier of the creator.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Data transfer object for an internal note.
/// </summary>
public class InternalNoteDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the author's name.</summary>
    public string Author { get; set; } = string.Empty;
    /// <summary>Gets or sets the note content.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Data transfer object for a file attachment.
/// </summary>
public class FileAttachmentDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the name of the file.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Gets or sets the storage path.</summary>
    public string StoragePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the type of the file.</summary>
    public string FileType { get; set; } = string.Empty;
    /// <summary>Gets or sets the size of the file in bytes.</summary>
    public long FileSize { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed order information.
/// </summary>
public class OrderDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the order identifier string.</summary>
    public string OrderId { get; set; } = string.Empty;
    /// <summary>Gets or sets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer ID.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the type of customer.</summary>
    public string CustomerType { get; set; } = string.Empty;
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets or sets the currency.</summary>
    public string Currency { get; set; } = "THB";
    /// <summary>Gets or sets the customer's purchase order number.</summary>
    public string? CustomerPoNumber { get; set; }
    /// <summary>Gets or sets the ID of the customer's PO file.</summary>
    public Guid? CustomerPoFileId { get; set; }
    /// <summary>Gets or sets the total quantity ordered.</summary>
    public int? OrderedQuantity { get; set; }
    /// <summary>Gets or sets the quantity manufactured so far.</summary>
    public int? ManufacturedQuantity { get; set; }
    /// <summary>Gets or sets the current production status.</summary>
    public string? CurrentStatus { get; set; }
    /// <summary>Gets or sets the quoted amount.</summary>
    public decimal? QuotedAmount { get; set; }
    /// <summary>Gets or sets the list of items in the order.</summary>
    public List<OrderItemDto> Items { get; set; } = [];
    /// <summary>Gets or sets the timeline of status changes.</summary>
    public List<OrderTimelineDto> Timeline { get; set; } = [];
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Data transfer object for an order item.
/// </summary>
public class OrderItemDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the item description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the internal product code.</summary>
    public string? ProductCode { get; set; }
    /// <summary>Gets or sets the ordered quantity.</summary>
    public decimal Quantity { get; set; }
    /// <summary>Gets or sets the unit price.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>Gets the total price for this item.</summary>
    public decimal TotalPrice => Quantity * UnitPrice;
    /// <summary>Gets or sets the manufacturing service type.</summary>
    public string? ServiceType { get; set; } // 3D Print, CNC, etc.
}

/// <summary>
/// Data transfer object for a point in an order's timeline.
/// </summary>
public class OrderTimelineDto
{
    /// <summary>Gets or sets the status reached at this point.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional note.</summary>
    public string? Note { get; set; }
    /// <summary>Gets or sets the identifier of the person who updated the status.</summary>
    public string? UpdatedBy { get; set; }
    /// <summary>Gets or sets the event timestamp.</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Request model for creating a new quotation.
/// </summary>
public sealed record CreateQuotationRequest
{
    /// <summary>Gets or sets the customer ID.</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the billing identity type.</summary>
    public BillingIdentityType BillingIdentityType { get; set; }

    /// <summary>Gets or sets the start of the validity period.</summary>
    [Required]
    public DateTime ValidityPeriodStart { get; set; }

    /// <summary>Gets or sets the end of the validity period.</summary>
    [Required]
    public DateTime ValidityPeriodEnd { get; set; }

    /// <summary>Gets or sets the list of items in the quotation.</summary>
    public List<QuotationItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the currency code (ISO 4217). Defaults to THB.</summary>
    public string CurrencyCode { get; set; } = "THB";

    /// <summary>Gets or sets delivery expectations.</summary>
    public string? DeliveryExpectations { get; set; }
}

/// <summary>
/// Data transfer object for a quotation line item.
/// </summary>
public sealed record QuotationItemDto
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
}

/// <summary>
/// Request model for updating an existing quotation.
/// </summary>
public sealed record UpdateQuotationRequest
{
    /// <summary>Gets or sets the updated status.</summary>
    public string? Status { get; set; }
    /// <summary>Gets or sets the updated validity date.</summary>
    public DateTime? ValidityDate { get; set; }
    /// <summary>Gets or sets the updated list of items.</summary>
    public List<QuotationItemDto>? Items { get; set; }
}

/// <summary>
/// Request model for adding an internal note to a quotation.
/// </summary>
public sealed record AddQuotationNoteRequest
{
    /// <summary>
    /// The note content to add.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating an order status.
/// </summary>
public sealed record UpdateOrderStatusRequest
{
    /// <summary>Gets or sets the new status.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating an order.
/// </summary>
public sealed record UpdateOrderRequest
{
    /// <summary>Gets or sets the customer's PO number.</summary>
    public string? CustomerPoNumber { get; set; }
    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
}
