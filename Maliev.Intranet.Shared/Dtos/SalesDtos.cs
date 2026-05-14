using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Represents a high-level summary of an order for list views and reporting.
/// </summary>
public class OrderSummaryDto
{
    /// <summary>The unique identifier of the order.</summary>
    public Guid Id { get; set; }

    /// <summary>The unique human-readable order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>The display name of the customer who placed the order.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>The total amount of the order before taxes or adjustments.</summary>
    public decimal Total { get; set; }

    /// <summary>The final total monetary amount of the order.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>The current fulfillment status of the order.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The date and time when the order was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Indicates whether this order is outsourced to an external manufacturing partner.
    /// Employee-only information.
    /// </summary>
    [JsonPropertyName("isOutsourced")]
    public bool IsOutsourced { get; set; }
}

/// <summary>
/// Represents a high-level summary of a quotation for list views and reporting.
/// </summary>
public class QuotationSummaryDto
{
    /// <summary>The unique identifier of the quotation.</summary>
    public Guid Id { get; set; }

    /// <summary>The unique human-readable quotation number.</summary>
    public string QuotationNumber { get; set; } = string.Empty;

    /// <summary>The display name of the customer receiving the quotation.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>The total calculated amount of the quotation.</summary>
    public decimal Total { get; set; }

    /// <summary>The date and time when the quotation was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed information for a specific quotation, including version history and attachments.
/// </summary>
public class QuotationDetailDto
{
    /// <summary>The unique identifier of the quotation record.</summary>
    public Guid Id { get; set; }

    /// <summary>The unique human-readable quotation number (e.g., QT-2023-001).</summary>
    public string QuotationNumber { get; set; } = string.Empty;

    /// <summary>The unique identifier of the associated customer.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>The display name of the customer.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>The identifier of the source Request for Quotation (RFQ), if applicable.</summary>
    public Guid? SourceRfqId { get; set; }

    /// <summary>The number of the source Request for Quotation (RFQ), if applicable.</summary>
    public string? SourceRfqNumber { get; set; }

    /// <summary>The source ProjectService project identifier when generated from a project workspace.</summary>
    public Guid? SourceProjectId { get; set; }

    /// <summary>The source ProjectService project number when generated from a project workspace.</summary>
    public string? SourceProjectNumber { get; set; }

    /// <summary>The sequence number of the current active version of the quotation.</summary>
    public int CurrentVersionNumber { get; set; }

    /// <summary>The current status of the quotation (e.g., Draft, Sent, Accepted, Rejected).</summary>
    [JsonConverter(typeof(QuotationStatusStringJsonConverter))]
    public string Status { get; set; } = string.Empty;

    /// <summary>The start date of the quotation's validity period.</summary>
    public DateTime ValidityPeriodStart { get; set; }

    /// <summary>The end date of the quotation's validity period.</summary>
    public DateTime ValidityPeriodEnd { get; set; }

    /// <summary>The calculated sub-total of all line items before tax.</summary>
    public decimal SubTotal { get; set; }

    /// <summary>The total tax amount applied to the quotation.</summary>
    public decimal Tax { get; set; }

    /// <summary>The final total amount of the quotation including tax.</summary>
    public decimal Total { get; set; }

    /// <summary>The ISO 4217 currency code for the quotation amounts.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>A description of the customer's delivery expectations or constraints.</summary>
    public string? DeliveryExpectations { get; set; }

    /// <summary>The full history of all versions created for this quotation.</summary>
    public List<QuotationVersionDto> Versions { get; set; } = [];

    /// <summary>Internal notes and communications related to the quotation.</summary>
    public List<InternalNoteDto> InternalNotes { get; set; } = [];

    /// <summary>Files and documents attached to the quotation.</summary>
    public List<FileAttachmentDto> Attachments { get; set; } = [];

    /// <summary>The date and time when the quotation record was first created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>The date and time when the quotation record was last modified.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Represents a specific version of a quotation with its own line items and totals.
/// </summary>
public class QuotationVersionDto
{
    /// <summary>The unique identifier of the quotation version record.</summary>
    public Guid Id { get; set; }

    /// <summary>The version number within the quotation's history.</summary>
    public int VersionNumber { get; set; }

    /// <summary>The collection of line items specific to this version of the quotation.</summary>
    public List<QuotationItemDto> LineItems { get; set; } = [];

    /// <summary>The total calculated price for this specific version.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>The discount structure applied to this quotation version, if any.</summary>
    public SalesDiscountStructureDto? DiscountStructure { get; set; }

    /// <summary>The manual discount amount applied to this quotation version.</summary>
    public decimal ManualDiscountAmount { get; set; }

    /// <summary>The shipping or delivery cost applied to this quotation version.</summary>
    public decimal ShippingCost { get; set; }

    /// <summary>The VAT or tax amount applied to this quotation version.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>The ISO 4217 currency code used for this version.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Updated delivery expectations for this specific version.</summary>
    public string? DeliveryExpectations { get; set; }

    /// <summary>A brief summary of what changed in this version compared to the previous one.</summary>
    public string? ChangeSummary { get; set; }

    /// <summary>Immutable JSON project snapshot captured for this quotation version.</summary>
    public string? ProjectSnapshotJson { get; set; }

    /// <summary>Deterministic hash of the immutable project snapshot.</summary>
    public string? ProjectSnapshotHash { get; set; }

    /// <summary>Customer-facing PDF artifact URL for this exact version.</summary>
    public string? PdfArtifactUrl { get; set; }

    /// <summary>Storage path for the PDF artifact for this exact version.</summary>
    public string? PdfArtifactStoragePath { get; set; }

    /// <summary>Timestamp when the PDF artifact was generated for this exact version.</summary>
    public DateTime? PdfGeneratedAt { get; set; }

    /// <summary>Human-readable display name of the user who generated this version.</summary>
    public string? GeneratedByDisplayName { get; set; }

    /// <summary>Customer-facing special terms shown on generated PDFs.</summary>
    public string? SpecialTerms { get; set; }

    /// <summary>The identifier of the user who created this version.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>The identifier of the user who created this version as returned by QuotationService.</summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>The date and time when this version was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents an internal note or comment attached to a business record.
/// </summary>
public class InternalNoteDto
{
    /// <summary>The unique identifier of the internal note.</summary>
    public Guid Id { get; set; }

    /// <summary>The name or identifier of the user who authored the note.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>The textual content of the internal note.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>The date and time when the note was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Converts QuotationService numeric enum status values into stable UI status names.
/// </summary>
public sealed class QuotationStatusStringJsonConverter : JsonConverter<string>
{
    /// <inheritdoc />
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.Number when reader.TryGetInt32(out var value) => value switch
            {
                1 => "Draft",
                2 => "PendingApproval",
                3 => "Approved",
                4 => "CustomerReview",
                5 => "Accepted",
                6 => "Expired",
                7 => "Cancelled",
                _ => value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            _ => throw new JsonException($"Cannot convert JSON token {reader.TokenType} to quotation status string.")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}

/// <summary>
/// Represents a file attachment associated with a business record.
/// </summary>
public class FileAttachmentDto
{
    /// <summary>The unique identifier of the file attachment record.</summary>
    public Guid Id { get; set; }

    /// <summary>The original name of the file.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>The internal path or URI where the file is stored.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>The MIME type or file extension category.</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>The size of the file in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>The date and time when the file was attached.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed information for a specific order, including item breakdown and fulfillment timeline.
/// </summary>
public class OrderDetailDto
{
    /// <summary>The unique identifier of the order record.</summary>
    public Guid Id { get; set; }

    /// <summary>The internal identifier of the order.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>The unique human-readable order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>The unique identifier of the customer associated with this order.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>The display name of the customer.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>The classification of the customer (e.g., "Individual", "Corporate").</summary>
    public string CustomerType { get; set; } = string.Empty;

    /// <summary>The overall fulfillment status of the order.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The final total monetary amount of the order.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>The ISO 4217 currency code for the order total.</summary>
    public string Currency { get; set; } = "THB";

    /// <summary>The customer's internal purchase order number, if provided.</summary>
    public string? CustomerPoNumber { get; set; }

    /// <summary>The identifier of the customer's uploaded purchase order file.</summary>
    public Guid? CustomerPoFileId { get; set; }

    /// <summary>The total number of units requested in the order.</summary>
    public int? OrderedQuantity { get; set; }

    /// <summary>The number of units that have successfully completed manufacturing.</summary>
    public int? ManufacturedQuantity { get; set; }

    /// <summary>The current detailed status of the order fulfillment.</summary>
    public string? CurrentStatus { get; set; }

    /// <summary>The original amount quoted to the customer before the order was placed.</summary>
    public decimal? QuotedAmount { get; set; }

    /// <summary>The list of individual product or service items within this order.</summary>
    public List<OrderItemDto> Items { get; set; } = [];

    /// <summary>A chronological history of status changes and events for this order.</summary>
    public List<OrderTimelineDto> Timeline { get; set; } = [];

    /// <summary>The date and time when the order was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>The date and time when the order record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Employee-only. True if order is outsourced.</summary>
    [JsonPropertyName("isOutsourced")]
    public bool IsOutsourced { get; set; }

    /// <summary>Employee-only. Supplier cost in THB for margin tracking.</summary>
    [JsonPropertyName("supplierCostTHB")]
    public decimal? SupplierCostTHB { get; set; }

    /// <summary>Employee-only. Outsourcing supplier name.</summary>
    [JsonPropertyName("supplierName")]
    public string? SupplierName { get; set; }

    /// <summary>Employee-only. Estimated delivery from supplier.</summary>
    [JsonPropertyName("supplierEstimatedDelivery")]
    public DateTime? SupplierEstimatedDelivery { get; set; }
}

/// <summary>
/// Represents an individual item or service line within an order.
/// </summary>
public class OrderItemDto
{
    /// <summary>The unique identifier of the order item record.</summary>
    public Guid Id { get; set; }

    /// <summary>A detailed description of the product or service provided.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The unique product code or SKU, if applicable.</summary>
    public string? ProductCode { get; set; }

    /// <summary>The quantity of items or services ordered.</summary>
    public decimal Quantity { get; set; }

    /// <summary>The price per unit for the specific item in this order.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>The total calculated price for this item (Quantity * UnitPrice).</summary>
    public decimal TotalPrice => Quantity * UnitPrice;

    /// <summary>The type of manufacturing or service process applied (e.g., "3D Print", "CNC").</summary>
    public string? ServiceType { get; set; }
}

/// <summary>
/// Represents a specific event or status change in the lifecycle of an order.
/// </summary>
public class OrderTimelineDto
{
    /// <summary>The status of the order at the time of this event.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>An optional descriptive note or explanation for the event.</summary>
    public string? Note { get; set; }

    /// <summary>The name or identifier of the user who triggered the status update.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>The date and time when the status update occurred.</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Discount structure returned by QuotationService for a quotation version.
/// </summary>
public sealed record SalesDiscountStructureDto
{
    /// <summary>The discount type, for example Percentage, FixedAmount, or VolumeBased.</summary>
    [JsonConverter(typeof(SalesDiscountTypeJsonConverter))]
    public SalesDiscountType DiscountType { get; set; }

    /// <summary>The discount value as an amount or percentage depending on <see cref="DiscountType"/>.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Optional conditions that explain why the discount applies.</summary>
    public string? Conditions { get; set; }

    /// <summary>Optional authorization reason recorded by QuotationService.</summary>
    public string? AuthorizationReason { get; set; }
}

/// <summary>
/// Discount types returned by QuotationService.
/// </summary>
public enum SalesDiscountType
{
    /// <summary>Percentage discount.</summary>
    Percentage = 1,

    /// <summary>Fixed amount discount.</summary>
    FixedAmount = 2,

    /// <summary>Volume-based discount.</summary>
    VolumeBased = 3,
}

/// <summary>
/// Converts QuotationService discount type values from either numeric enum values or names.
/// </summary>
public sealed class SalesDiscountTypeJsonConverter : JsonConverter<SalesDiscountType>
{
    /// <inheritdoc />
    public override SalesDiscountType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt32(out var value) && Enum.IsDefined(typeof(SalesDiscountType), value) => (SalesDiscountType)value,
            JsonTokenType.String when Enum.TryParse<SalesDiscountType>(reader.GetString(), ignoreCase: true, out var value) => value,
            _ => SalesDiscountType.FixedAmount,
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, SalesDiscountType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// Data required to create a new quotation for a customer.
/// </summary>
public sealed record CreateQuotationRequest
{
    /// <summary>The identifier of the customer for whom the quotation is created.</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>Specifies the type of legal identity used for billing purposes.</summary>
    public BillingIdentityType BillingIdentityType { get; set; }

    /// <summary>The requested start date for the quotation's validity period.</summary>
    [Required]
    public DateTime ValidityPeriodStart { get; set; }

    /// <summary>The requested expiration date for the quotation's validity period.</summary>
    [Required]
    public DateTime ValidityPeriodEnd { get; set; }

    /// <summary>The list of product or service items to include in the quotation.</summary>
    public List<QuotationItemDto> Items { get; set; } = [];

    /// <summary>A description of delivery requirements or expectations.</summary>
    public string? DeliveryExpectations { get; set; }
}

/// <summary>
/// Represents an individual item or service line within a quotation.
/// </summary>
public sealed record QuotationItemDto
{
    /// <summary>A description of the product or service being quoted.</summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>The quantity of items or services being quoted.</summary>
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    /// <summary>The proposed price per unit.</summary>
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Data required to update an existing quotation.
/// </summary>
public sealed record UpdateQuotationRequest
{
    /// <summary>The new status to assign to the quotation.</summary>
    public string? Status { get; set; }

    /// <summary>The updated expiration date for the quotation.</summary>
    public DateTime? ValidityDate { get; set; }

    /// <summary>The updated list of quotation line items.</summary>
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
/// Request model for updating the fulfillment status of an order.
/// </summary>
public sealed record UpdateOrderStatusRequest
{
    /// <summary>The new status to assign to the order (e.g., "Processing", "Shipped").</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Data required to update general order information.
/// </summary>
public sealed record UpdateOrderRequest
{
    /// <summary>The customer's internal purchase order number.</summary>
    public string? CustomerPoNumber { get; set; }

    /// <summary>General notes or instructions regarding the order.</summary>
    public string? Notes { get; set; }
}
