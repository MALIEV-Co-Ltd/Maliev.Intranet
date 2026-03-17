using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Order summary information.
/// </summary>
public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Quotation summary DTO.
/// </summary>
public class QuotationSummaryDto
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed quotation information.
/// </summary>
public class QuotationDetailDto
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? SourceRfqId { get; set; }
    public string? SourceRfqNumber { get; set; }
    public int CurrentVersionNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ValidityPeriodStart { get; set; }
    public DateTime ValidityPeriodEnd { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? DeliveryExpectations { get; set; }
    public List<QuotationVersionDto> Versions { get; set; } = [];
    public List<InternalNoteDto> InternalNotes { get; set; } = [];
    public List<FileAttachmentDto> Attachments { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class QuotationVersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public List<QuotationItemDto> LineItems { get; set; } = [];
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? DeliveryExpectations { get; set; }
    public string? ChangeSummary { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class InternalNoteDto
{
    public Guid Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class FileAttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed order information.
/// </summary>
public class OrderDetailDto
{
    public Guid Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "THB";
    public string? CustomerPoNumber { get; set; }
    public Guid? CustomerPoFileId { get; set; }
    public int? OrderedQuantity { get; set; }
    public int? ManufacturedQuantity { get; set; }
    public string? CurrentStatus { get; set; }
    public decimal? QuotedAmount { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
    public List<OrderTimelineDto> Timeline { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    public string? ServiceType { get; set; } // 3D Print, CNC, etc.
}

public class OrderTimelineDto
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed record CreateQuotationRequest
{
    [Required]
    public Guid CustomerId { get; set; }

    public BillingIdentityType BillingIdentityType { get; set; }

    [Required]
    public DateTime ValidityPeriodStart { get; set; }

    [Required]
    public DateTime ValidityPeriodEnd { get; set; }

    public List<QuotationItemDto> Items { get; set; } = [];

    public string? DeliveryExpectations { get; set; }
}

public sealed record QuotationItemDto
{
    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

public sealed record UpdateQuotationRequest
{
    public string? Status { get; set; }
    public DateTime? ValidityDate { get; set; }
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

public sealed record UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public sealed record UpdateOrderRequest
{
    public string? CustomerPoNumber { get; set; }
    public string? Notes { get; set; }
}
