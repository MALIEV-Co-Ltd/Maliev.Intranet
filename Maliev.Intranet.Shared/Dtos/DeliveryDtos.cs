using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Represents a summary of a delivery note for listing purposes.
/// </summary>
public sealed record DeliveryNoteSummaryDto
{
    /// <summary>Gets or sets the unique identifier of the delivery note.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the service-owned delivery note identifier.</summary>
    public string DeliveryNoteId { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable delivery note number.</summary>
    public string DeliveryNoteNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the associated order identifier.</summary>
    public string? OrderId { get; set; }

    /// <summary>Gets or sets the associated purchase order identifier.</summary>
    public int? PurchaseOrderId { get; set; }

    /// <summary>Gets or sets the associated order number.</summary>
    public string? OrderNumber { get; set; }

    /// <summary>Gets or sets the unique identifier of the customer.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the name of the customer receiving the delivery.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Gets or sets the scheduled or actual delivery date.</summary>
    public DateTime DeliveryDate { get; set; }

    /// <summary>Gets or sets the current status of the delivery (e.g., "Shipped", "Delivered").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the shipping carrier.</summary>
    public string? CarrierName { get; set; }

    /// <summary>Gets or sets the tracking number for the shipment.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the number of items included in this delivery.</summary>
    public int ItemCount { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the record was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents the detailed information of a delivery note.
/// </summary>
public sealed record DeliveryNoteDetailDto
{
    /// <summary>Gets or sets the unique identifier of the delivery note.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the service-owned delivery note identifier.</summary>
    public string DeliveryNoteId { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable delivery note number.</summary>
    public string DeliveryNoteNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier of the associated order.</summary>
    public string? OrderId { get; set; }

    /// <summary>Gets or sets the associated purchase order identifier.</summary>
    public int? PurchaseOrderId { get; set; }

    /// <summary>Gets or sets the associated order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier of the customer.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the name of the customer receiving the delivery.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Gets or sets the billing or primary address of the customer.</summary>
    public string CustomerAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduled delivery date.</summary>
    public DateTime DeliveryDate { get; set; }

    /// <summary>Gets or sets the actual time when the delivery was completed.</summary>
    public DateTime? ActualDeliveryTime { get; set; }

    /// <summary>Gets or sets the current status of the delivery.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the tracking number for the shipment.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the name of the shipping carrier.</summary>
    public string? CarrierName { get; set; }

    /// <summary>Gets or sets the name of the contact person at the delivery location.</summary>
    public string? DeliveryContactName { get; set; }

    /// <summary>Gets or sets the phone number of the contact person at the delivery location.</summary>
    public string? DeliveryContactPhone { get; set; }

    /// <summary>Gets or sets the first line of the shipping address.</summary>
    public string? ShippingAddressLine1 { get; set; }

    /// <summary>Gets or sets the second line of the shipping address.</summary>
    public string? ShippingAddressLine2 { get; set; }

    /// <summary>Gets or sets the city of the shipping address.</summary>
    public string? ShippingCity { get; set; }

    /// <summary>Gets or sets the province or state of the shipping address.</summary>
    public string? ShippingProvince { get; set; }

    /// <summary>Gets or sets the postal code of the shipping address.</summary>
    public string? ShippingPostalCode { get; set; }

    /// <summary>Gets or sets the country of the shipping address.</summary>
    public string? ShippingCountry { get; set; }

    /// <summary>Gets or sets special instructions for the delivery.</summary>
    public string? DeliveryInstructions { get; set; }

    /// <summary>Gets or sets internal notes for the delivery staff.</summary>
    public string? InternalNotes { get; set; }

    /// <summary>Gets or sets the delivery contact email address.</summary>
    public string? DeliveryContactEmail { get; set; }

    /// <summary>Gets or sets the shipping cost.</summary>
    public decimal? ShippingCost { get; set; }

    /// <summary>Gets or sets the shipping cost currency.</summary>
    public string? ShippingCostCurrency { get; set; }

    /// <summary>Gets or sets the timestamp when delivery evidence was signed.</summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>Gets or sets the name of the person who received the delivery.</summary>
    public string? ReceivedByName { get; set; }

    /// <summary>Gets or sets the signature file identifier linked to proof of delivery.</summary>
    public Guid? SignatureFileId { get; set; }

    /// <summary>Gets or sets the collection of items included in this delivery.</summary>
    public List<DeliveryNoteItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the UTC timestamp when the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the name of the user who created the record.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the record was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the name of the user who last updated the record.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Gets or sets the concurrency version returned by the service.</summary>
    public uint Version { get; set; }
}

/// <summary>
/// Represents an item entry within a delivery note.
/// </summary>
public sealed record DeliveryNoteItemDto
{
    /// <summary>Gets or sets the unique identifier of the delivery note item.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the associated order identifier.</summary>
    public string? OrderId { get; set; }

    /// <summary>Gets or sets the associated purchase order item identifier.</summary>
    public int? PurchaseOrderItemId { get; set; }

    /// <summary>Gets or sets the unique product code.</summary>
    public string? ProductCode { get; set; }

    /// <summary>Gets or sets the display name of the product.</summary>
    public string? ProductName { get; set; }

    /// <summary>Gets or sets a detailed description of the product.</summary>
    public string? ProductDescription { get; set; }

    /// <summary>Gets or sets the total quantity originally ordered.</summary>
    public decimal QuantityOrdered { get; set; }

    /// <summary>Gets or sets the quantity that has been manufactured and is ready for delivery.</summary>
    public decimal QuantityManufactured { get; set; }

    /// <summary>Gets or sets the quantity included in this delivery.</summary>
    public decimal QuantityDelivered { get; set; }

    /// <summary>Gets or sets the unit of measure for the quantity (e.g., "pcs", "kg").</summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>Gets or sets additional notes for this specific item in the delivery.</summary>
    public string? ItemNotes { get; set; }

    /// <summary>Gets or sets the concurrency version returned by the service.</summary>
    public uint Version { get; set; }
}

/// <summary>
/// Request payload for creating a new delivery note from an existing order.
/// </summary>
public sealed record CreateDeliveryNoteRequest
{
    /// <summary>Gets or sets the unique identifier of the order to create a delivery note for.</summary>
    public string? OrderId { get; set; }

    /// <summary>Gets or sets the purchase order identifier to create a delivery note for.</summary>
    public int? PurchaseOrderId { get; set; }

    /// <summary>Gets or sets the unique identifier of the customer receiving the delivery.</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the name of the customer receiving the delivery.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Gets or sets the scheduled delivery date.</summary>
    [Required]
    public DateTime DeliveryDate { get; set; }

    /// <summary>Gets or sets the optional tracking number for the shipment.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the name of the shipping carrier.</summary>
    public string? CarrierName { get; set; }

    /// <summary>Gets or sets the name of the contact person at the delivery location.</summary>
    public string? DeliveryContactName { get; set; }

    /// <summary>Gets or sets the phone number of the contact person at the delivery location.</summary>
    public string? DeliveryContactPhone { get; set; }

    /// <summary>Gets or sets the email address of the contact person at the delivery location.</summary>
    public string? DeliveryContactEmail { get; set; }

    /// <summary>Gets or sets the first line of the shipping address.</summary>
    public string? ShippingAddressLine1 { get; set; }

    /// <summary>Gets or sets the second line of the shipping address.</summary>
    public string? ShippingAddressLine2 { get; set; }

    /// <summary>Gets or sets the city of the shipping address.</summary>
    public string? ShippingCity { get; set; }

    /// <summary>Gets or sets the province or state of the shipping address.</summary>
    public string? ShippingProvince { get; set; }

    /// <summary>Gets or sets the postal code of the shipping address.</summary>
    public string? ShippingPostalCode { get; set; }

    /// <summary>Gets or sets the country of the shipping address.</summary>
    public string? ShippingCountry { get; set; }

    /// <summary>Gets or sets the shipping cost.</summary>
    public decimal? ShippingCost { get; set; }

    /// <summary>Gets or sets the shipping cost currency.</summary>
    public string? ShippingCostCurrency { get; set; }

    /// <summary>Gets or sets additional notes or instructions for the delivery.</summary>
    public string? DeliveryInstructions { get; set; }

    /// <summary>Gets or sets the collection of items to be included in the delivery.</summary>
    public List<CreateDeliveryNoteItemRequest> Items { get; set; } = [];
}

/// <summary>
/// Request payload for an individual item to be included in a new delivery note.
/// </summary>
public sealed record CreateDeliveryNoteItemRequest
{
    /// <summary>Gets or sets the associated order identifier.</summary>
    public string? OrderId { get; set; }

    /// <summary>Gets or sets the associated purchase order item identifier.</summary>
    public int? PurchaseOrderItemId { get; set; }

    /// <summary>Gets or sets the unique product code.</summary>
    [Required]
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the product.</summary>
    [Required]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Gets or sets a detailed description of the product.</summary>
    public string? ProductDescription { get; set; }

    /// <summary>Gets or sets the total quantity originally ordered.</summary>
    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal QuantityOrdered { get; set; }

    /// <summary>Gets or sets the quantity that has been manufactured and is ready for delivery.</summary>
    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal QuantityManufactured { get; set; }

    /// <summary>Gets or sets the quantity included in this delivery.</summary>
    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal QuantityDelivered { get; set; }

    /// <summary>Gets or sets the unit of measure for the quantity.</summary>
    [Required]
    public string UnitOfMeasure { get; set; } = "pcs";

    /// <summary>Gets or sets additional notes for this item.</summary>
    public string? ItemNotes { get; set; }
}

/// <summary>
/// Request payload for updating the status of an existing delivery.
/// </summary>
public sealed record UpdateDeliveryStatusRequest
{
    /// <summary>Gets or sets the new status of the delivery (e.g., "Delivered", "Cancelled").</summary>
    [Required]
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the person who received the delivery.</summary>
    public string? ReceivedByName { get; set; }

    /// <summary>Gets or sets the actual time when the delivery was received.</summary>
    public DateTime? ActualDeliveryTime { get; set; }

    /// <summary>Gets or sets the signature file identifier linked to proof of delivery.</summary>
    public Guid? SignatureFileId { get; set; }
}

/// <summary>
/// Response returned when delivery note PDF generation is queued.
/// </summary>
public sealed record DeliveryPdfRequestResponse
{
    /// <summary>Gets or sets the delivery note identifier.</summary>
    public string DeliveryNoteId { get; set; } = string.Empty;

    /// <summary>Gets or sets the queued PDF request status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the status message returned by the service.</summary>
    public string? Message { get; set; }
}
